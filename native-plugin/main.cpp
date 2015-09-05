#include <iostream>
#include <time.h> 
#include <windows.h>
#include <functional>

#include "xmpdsp.h"
#include "xmpfunc.h"

#include "SharpScrobblerWrapper.h"
#include "main.h"

// > 30 seconds
#define TRACK_DURATION_THRESHOLD_MS 30 * 1000
// half the track, or 4min, whichever comes first
#define TRACK_PLAY_TIME_THRESHOLD_MS 4 * 60 * 1000

static XMPFUNC_MISC* xmpfmisc;
static XMPFUNC_STATUS* xmpfstatus;

static ScrobblerConfig scrobblerConf;

static SharpScrobblerWrapper* scrobbler = NULL;

static TrackInfo* currentTrackInfo = NULL;

// Sample rate of the current track, by 1000.
static DWORD xmprateBy1000 = 0;
// Total number of samples processed by the DSP for the current track.
static DWORD processedSamplesForCurrentTrack = 0;
// Number of samples processed by the DSP for the current track since the last reset.
// (A reset is triggered when the user manually seeks into the track)
static DWORD processedSamplesForCurrentTrackSinceLastReset = 0;
// Internal threshold used to debounce the calls to TrackStartsPlaying()
static int msThresholdForNewTrack = 0;
// Expected number of ms to the end of the current track from the last reset.
// (Or from the beginning of the track if there has been no reset)
static int expectedEndOfCurrentTrackInMs = INT_MAX;
// Used as a sanity check the loop detection.
static double lastMeasuredTrackPosition = NAN;
// Total duration of the current track.
static int currentTrackDurationMs = 0;
//
static bool scrobbleCurrentTrackInfoOnEnd = false;

static XMPDSP dsp =
{
    XMPDSP_FLAG_NODSP,
    "XMPlay Sharp Scrobbler",
    DSP_About,
    DSP_New,
    DSP_Free,
    DSP_GetDescription,
    DSP_Config,
    DSP_GetConfig,
    DSP_SetConfig,
    DSP_NewTrack,
    DSP_SetFormat,
    DSP_Reset,
    DSP_Process,
    DSP_NewTitle
};

static void ExecuteOnManagedCallsThread(PTP_SIMPLE_CALLBACK workLoad)
{
    TrySubmitThreadpoolCallback(workLoad, NULL, NULL);
}

static void WINAPI DSP_About(HWND win)
{
    ExecuteOnManagedCallsThread([](PTP_CALLBACK_INSTANCE, void *)
    {
        SharpScrobblerWrapper::Initialize();

        int fortytwo = 42;
    });
    MessageBox(win,
        "XMPlay éµ\n",
        "XMPlay Sharp Scrobbler",
        MB_ICONINFORMATION);
}

static const char* WINAPI DSP_GetDescription(void* inst)
{
    return dsp.name;
}

static void* WINAPI DSP_New()
{
    // Force early initialization of the wrapper and the managed assemblies
    // to avoid concurrency errors if we keep the lazy loading behavior.
    // We still do this on a new thread to avoid slowing down XMPlay startup
    ExecuteOnManagedCallsThread([](PTP_CALLBACK_INSTANCE, void *)
    {
        scrobbler = new SharpScrobblerWrapper();
    });

    return (void*)1;
}

static void WINAPI DSP_Free(void* inst)
{
    ReleaseTrackInfo(currentTrackInfo);
    delete scrobbler;
}

// Called after a click on the plugin Config button.
static void WINAPI DSP_Config(void* inst, HWND win)
{
    const char* sessionKey = SharpScrobblerWrapper::AskUserForNewAuthorizedSessionKey();
    memcpy(scrobblerConf.sessionKey, sessionKey, sizeof(scrobblerConf.sessionKey));
}

// Get config from the plugin. (return size of config data)
static DWORD WINAPI DSP_GetConfig(void* inst, void* config)
{
    memcpy(config, &scrobblerConf, sizeof(ScrobblerConfig));
    return sizeof(ScrobblerConfig); // return size of config info
}

// Apply config to the plugin.
static BOOL WINAPI DSP_SetConfig(void* inst, void* config, DWORD size)
{
    memcpy(&scrobblerConf, config, sizeof(ScrobblerConfig));
    return TRUE;
}

// Called when a track has been opened or closed.
// (file will be NULL in the later case)
static void WINAPI DSP_NewTrack(void* inst, const char* file)
{
    ResetForNewTrack();
}

// Called when a format is set (because of a new track for example)
// (if form is NULL, output stopped)
static void WINAPI DSP_SetFormat(void* inst, const XMPFORMAT* form)
{
    ResetForNewTrack();
    if (form != NULL)
    {
        xmprateBy1000 = form->rate / 1000;
    }
    else
    {
        xmprateBy1000 = 0;
    }
}

// This is apparently useless as I have never seen it called once.
static void WINAPI DSP_NewTitle(void* inst, const char* title) { }

// Called when the user seeks into the track.
static void WINAPI DSP_Reset(void* inst)
{
    double resetPosition = xmpfstatus->GetTime();
    expectedEndOfCurrentTrackInMs = GetExpectedEndOfCurrentTrackInMs((int)resetPosition * 1000);
    processedSamplesForCurrentTrackSinceLastReset = 0;
    // Reinitialize the last measured position to avoid an improbable bug in the case where
    // the user would seek *just* before the track looping.
    lastMeasuredTrackPosition = NAN;
}

static DWORD WINAPI DSP_Process(void* inst, float* data, DWORD count)
{
    // Check whether the processed track is a track just starting to play or not:
    if (processedSamplesForCurrentTrack == 0) msThresholdForNewTrack = count / xmprateBy1000;
    int calculatedPlayedMs = processedSamplesForCurrentTrack / xmprateBy1000;
    if (calculatedPlayedMs < msThresholdForNewTrack)
    {
        // new track starts playing normally
        currentTrackDurationMs = expectedEndOfCurrentTrackInMs = GetExpectedEndOfCurrentTrackInMs(0);
        InitializeCurrentTrackInfo();
        TrackStartsPlaying();
        scrobbleCurrentTrackInfoOnEnd = false;
    }
    else
    {
        // in the middle of a track. Before continuing, we check whether the track has looped without XMPlay telling us
        int calculatedPlayedMsSinceLastReset = processedSamplesForCurrentTrackSinceLastReset / xmprateBy1000;
        // If the track duration is 0, this is a stream. It's useless to check for a loop in that case, so we skip all the next part.
        if (currentTrackDurationMs != 0 && calculatedPlayedMsSinceLastReset > expectedEndOfCurrentTrackInMs)
        {
            // the calculated play time, based on the number of samples actually played,
            // is superior to the length of the track, but XMPlay did not signal a new track.
            // this means the track was looped, so this is effectively a new play.
            // so we reset everything like if it were a new track:

            // BUT
            // before doing that, we check the actual position reported by XMPlay.
            // (unlike the track duration, the position seems to be correctly synchronized to the current sample being processed)
            // We do that because otherwise, even with a full second being added to expectedEndOfCurrentTrackInMs to account for errors, the loop detection is often off by a few samples.
            // (a loop is believed to have been detected, followed almost immediately by an actual new track being played)
            // This might be because of gap-less transitions between tracks, the impossibility to precisely detect the length of a track in some file formats, or this might be a bug...
            // Anyway, for now, I trust the XMPlay reported position more than the calculations based on the number of samples having been played.

            double trustWorthyPosition = xmpfstatus->GetTime();
            if (isnan(lastMeasuredTrackPosition))
            {
                // this is the first time we get here.
                lastMeasuredTrackPosition = trustWorthyPosition;
            }
            else
            {
                // we've already been here...
                if (trustWorthyPosition >= lastMeasuredTrackPosition)
                {
                    // ...and the position increased, so we're not done yet playing the track.
                    lastMeasuredTrackPosition = trustWorthyPosition;
                    // Nothing to see here, this is not a looping yet...
                }
                else
                {
                    // ...and the position reset, this is an actual loop!
                    ResetForNewTrack();
                    // so that we don't risk detecting the new play a second time based on the processed samples count
                    msThresholdForNewTrack = 0;
                    currentTrackDurationMs = expectedEndOfCurrentTrackInMs = GetExpectedEndOfCurrentTrackInMs(0);
                    InitializeCurrentTrackInfo();
                    TrackStartsPlaying();
                    scrobbleCurrentTrackInfoOnEnd = false;
                    calculatedPlayedMs = 0;
                }
            }
        }
    }

    // Check whether the current track can be scrobbled:
    // The track must be longer than 30 seconds.
    // And the track has been played for at least half its duration, or for 4 minutes(whichever occurs earlier.)
    if (scrobbleCurrentTrackInfoOnEnd == false
        && currentTrackDurationMs > TRACK_DURATION_THRESHOLD_MS
        && (calculatedPlayedMs > (currentTrackDurationMs / 2) || calculatedPlayedMs >= TRACK_PLAY_TIME_THRESHOLD_MS))
    {
        scrobbleCurrentTrackInfoOnEnd = true;
    }

    processedSamplesForCurrentTrack += count;
    processedSamplesForCurrentTrackSinceLastReset += count;
    return count;
}

// Prepare everything for a new current track.
// (current number of processed samples, expected end of track...)
static void ResetForNewTrack()
{
    if (scrobbleCurrentTrackInfoOnEnd)
    {
        // scrobble previous track
        ScrobbleTrack();
        scrobbleCurrentTrackInfoOnEnd = false;
    }
    processedSamplesForCurrentTrack = 0;
    processedSamplesForCurrentTrackSinceLastReset = 0;
    lastMeasuredTrackPosition = NAN;
    // temporary safe upper value
    // (we might not have a track playing yet, so we can't calculate the correct value)
    expectedEndOfCurrentTrackInMs = INT_MAX;
}

// Calculate the expected time until the end of the current track from the desired position,
// and set it to expectedEndOfCurrentTrackInMs.
static int GetExpectedEndOfCurrentTrackInMs(int fromPositionMs)
{
    char* stringDuration = xmpfmisc->GetTag(TAG_LENGTH);
    double parsed = atof(stringDuration);
    int currentTrackMaxExpectedDurationMs = (int)(parsed * 1000);
    xmpfmisc->Free(stringDuration);
    return currentTrackMaxExpectedDurationMs - fromPositionMs;
}

// Get a wide string from an ansi string (don't forget to free it)
static wchar_t* GetStringW(const char* string)
{
    if (string == NULL)
    {
        return NULL;
    }
    else
    {
        size_t requiredSize = Utf2Uni(string, -1, NULL, 0);
        wchar_t* buffer = new wchar_t[requiredSize];
        Utf2Uni(string, -1, buffer, requiredSize);
        return buffer;
    }
}

// Get an XMPlay tag as a wide string (don't forget to free it)
static wchar_t* GetTagW(const char* tag)
{
    char* value = xmpfmisc->GetTag(tag);
    wchar_t* wValue = NULL;
    if (value != NULL)
    {
        wValue = GetStringW(value);
        xmpfmisc->Free(value);
    }
    return wValue;
}

static void InitializeCurrentTrackInfo()
{
    ReleaseTrackInfo(currentTrackInfo);

    TrackInfo* trackInfo = new TrackInfo();
    trackInfo->playStartTimestamp = time(NULL);
    trackInfo->title = GetTagW(TAG_TITLE);
    trackInfo->artist = GetTagW(TAG_ARTIST);
    trackInfo->album = GetTagW(TAG_ALBUM);

    // If the subsong tag is set, we use it instead of the track tag,
    // because inside a multi-track, the subsong number is more accurate.
    char* subsongNumber = xmpfmisc->GetTag(TAG_SUBSONG);// separated subsong (number/total)
    if (subsongNumber != NULL)
    {
        trackInfo->trackNumber = GetStringW(subsongNumber);
        xmpfmisc->Free(subsongNumber);
    }
    else
    {
        trackInfo->trackNumber = GetTagW(TAG_TRACK);
    }

    currentTrackInfo = trackInfo;
}

static void ReleaseTrackInfo(TrackInfo* trackInfo)
{
    if (trackInfo != NULL)
    {
        delete[] trackInfo->title;
        delete[] trackInfo->artist;
        delete[] trackInfo->album;
        delete[] trackInfo->trackNumber;
        delete trackInfo;
        trackInfo = NULL;
    }
}

// Called when a track starts playing.
// This differs from DSP_NewTrack() in that it correctly accounts for looped tracks.
// (That is, if a track loops, this function is called whereas DSP_NewTrack() is not)
static void TrackStartsPlaying()
{
    scrobbler->SetSessionKey(scrobblerConf.sessionKey);
    scrobbler->NowPlaying(
        currentTrackInfo->artist,
        currentTrackInfo->title,
        currentTrackInfo->album,
        currentTrackDurationMs,
        currentTrackInfo->trackNumber,
        NULL);
}

static void ScrobbleTrack()
{
    scrobbler->SetSessionKey(scrobblerConf.sessionKey);
    scrobbler->Scrobble(
        currentTrackInfo->artist,
        currentTrackInfo->title,
        currentTrackInfo->album,
        currentTrackDurationMs,
        currentTrackInfo->trackNumber,
        NULL,
        currentTrackInfo->playStartTimestamp);
}

// get the plugin's XMPDSP interface
XMPDSP* WINAPI XMPDSP_GetInterface2(DWORD face, InterfaceProc faceproc)
{
    if (face != XMPDSP_FACE) return NULL;
    xmpfmisc = (XMPFUNC_MISC*)faceproc(XMPFUNC_MISC_FACE); // import misc functions
    xmpfstatus = (XMPFUNC_STATUS*)faceproc(XMPFUNC_STATUS_FACE); // import playback status functions
    return &dsp;
}

BOOL WINAPI DllMain(HINSTANCE hDLL, DWORD reason, LPVOID reserved)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls(hDLL);
    }
    return 1;
}
