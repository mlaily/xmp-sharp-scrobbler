// Copyright(c) 2015-2016 Melvyn Laïly
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
#include <iostream>
#include <time.h> 
#include <windows.h>
#include "Commctrl.h"

#include "xmpdsp.h"
#include "xmpfunc.h"

#include "SharpScrobblerWrapper.h"
#include "main.h"

// > 30 seconds ?
#define TRACK_DURATION_THRESHOLD_MS     30 * 1000
// Half the track, or 4 minutes, whichever comes first.
#define TRACK_PLAY_TIME_THRESHOLD_MS    4 * 60 * 1000
// Magic number for MultiByteToWideChar()
// to auto detect the length of a null terminated source string
#define AUTO_NULL_TERMINATED_LENGTH     -1

static HINSTANCE hDll;

static XMPFUNC_MISC* xmpfmisc;
static XMPFUNC_STATUS* xmpfstatus;

static ScrobblerConfig scrobblerConf;

static SharpScrobblerWrapper* scrobbler = NULL;

static const char* currentFilePath = NULL;

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
// When set to true, the current track will be scrobbled as soon as it ends.
// Used to debounce the calls to OnTrackCompletes()
static bool scrobbleCurrentTrackInfoOnEnd = false;
// When set to true, a log will be added when a new track starts playing
// to indicate the current track was not played long enough to be scrobbled.
static bool logCurrentTrackWontScrobbleOnNextTrack = false;

static XMPDSP dsp =
{
    XMPDSP_FLAG_NODSP,
    PLUGIN_FRIENDLY_NAME,
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

static void WINAPI ShowInfoBubble(const char* text, int displayTimeMs)
{
    xmpfmisc->ShowBubble(text, displayTimeMs);
}

static void WINAPI DSP_About(HWND win)
{
    // Native dialog allowing to download .Net
    DialogBox(hDll, MAKEINTRESOURCE(IDD_ABOUT), win, &AboutDialogProc);
}

static BOOL CALLBACK AboutDialogProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    switch (msg)
    {
    case WM_INITDIALOG:
        SetDlgItemText(hWnd, IDC_ABOUT_DOTNET_LINK, ABOUT_DIALOG_TEXT);
        break;
    case WM_NOTIFY:
    {
        LPNMHDR pnmh = (LPNMHDR)lParam;
        if (pnmh->idFrom == IDC_ABOUT_DOTNET_LINK)
        {
            // Check for click or return key.
            if ((pnmh->code == NM_CLICK) || (pnmh->code == NM_RETURN))
            {
                // Take the user to the .Net 4.6 offline installer download page.
                ShellExecute(NULL, "open", "http://www.microsoft.com/en-us/download/details.aspx?id=48137", NULL, NULL, SW_SHOWNORMAL);
            }
        }
        break;
    }
    case WM_COMMAND:
        switch (LOWORD(wParam))
        {
        case IDOK:
        case IDCANCEL:
            EndDialog(hWnd, 0);
            break;
        }
        break;
    }
    return FALSE;
}

static const char* WINAPI DSP_GetDescription(void* inst)
{
    return PLUGIN_FRIENDLY_NAME;
}

static void* WINAPI DSP_New()
{
    scrobbler = new SharpScrobblerWrapper();
    SharpScrobblerWrapper::InitializeShowBubbleInfo(ShowInfoBubble);
    SharpScrobblerWrapper::LogInfo("****************************************************************************************************");
    SharpScrobblerWrapper::LogInfo(PLUGIN_FRIENDLY_NAME " " PLUGIN_VERSION_STRING " started!");
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
    const char* sessionKey = scrobbler->AskUserForNewAuthorizedSessionKey(win);
    if (sessionKey != NULL)
    {
        // If the new session key is valid, save it.
        memcpy(scrobblerConf.sessionKey, sessionKey, sizeof(scrobblerConf.sessionKey));
        scrobbler->SetSessionKey(scrobblerConf.sessionKey);
    }
}

// Get config from the plugin. (return size of config data)
static DWORD WINAPI DSP_GetConfig(void* inst, void* config)
{
    memcpy(config, &scrobblerConf, sizeof(ScrobblerConfig));
    return sizeof(ScrobblerConfig);
}

// Apply config to the plugin.
static BOOL WINAPI DSP_SetConfig(void* inst, void* config, DWORD size)
{
    memcpy(&scrobblerConf, config, sizeof(ScrobblerConfig));
    scrobbler->SetSessionKey(scrobblerConf.sessionKey);
    return TRUE;
}

// Called when a track has been opened or closed.
// (file will be NULL in the latter case)
static void WINAPI DSP_NewTrack(void* inst, const char* file)
{
    currentFilePath = file;
    CompleteCurrentTrack();
}

// Called when a format is set (because of a new track for example)
// (if form is NULL, output stopped)
static void WINAPI DSP_SetFormat(void* inst, const XMPFORMAT* form)
{
    CompleteCurrentTrack();
    if (form != NULL)
        xmprateBy1000 = form->rate / 1000;
    else
        xmprateBy1000 = 0;
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

// Strictly speaking, this plugin doesn't do any "processing", and always let the data untouched.
// Nonetheless, this function is useful to count every sample played and keep track of the play time,
// And also to detect tracks starting or looping, and act accordingly...
static DWORD WINAPI DSP_Process(void* inst, float* data, DWORD count)
{
    // Check whether the processed track is a track just starting to play or not:
    if (processedSamplesForCurrentTrack == 0) msThresholdForNewTrack = count / xmprateBy1000;
    int calculatedPlayedMs = processedSamplesForCurrentTrack / xmprateBy1000;
    if (calculatedPlayedMs < msThresholdForNewTrack)
    {
        // New track starts playing normally.
        TrackStartsPlaying();
    }
    else
    {
        // In the middle of a track. Before continuing, we check whether the track has looped without XMPlay telling us.
        int calculatedPlayedMsSinceLastReset = processedSamplesForCurrentTrackSinceLastReset / xmprateBy1000;
        // If the track duration is 0, this is a stream. It's useless to check for a loop in that case, so we skip the next part entirely.
        if (currentTrackDurationMs != 0 && calculatedPlayedMsSinceLastReset > expectedEndOfCurrentTrackInMs)
        {
            // The calculated play time based on the number of samples actually played
            // is superior to the length of the track, but XMPlay did not signal a new track.
            // This means the track is looping, so this is effectively a new play.
            // So we reset everything like if it were a new track:

            // BUT
            // before doing that, we check the actual position reported by XMPlay.
            // (Unlike the track duration, the position seems to be correctly synchronized to the sample currently being processed)
            // We do that because otherwise, even with a full second being added to expectedEndOfCurrentTrackInMs to account for errors, the loop detection is often off by a few samples.
            // (A loop is believed to have been detected, followed almost immediately by an actual new track being played)
            // This might be because of gap-less transitions between tracks, the impossibility to precisely detect the length of a track in some file formats, or this might simply be a bug...
            // Anyway, for now, I trust the XMPlay reported position more than the calculations based on the number of samples having been played.

            double trustWorthyPosition = xmpfstatus->GetTime();
            if (isnan(lastMeasuredTrackPosition))
            {
                // This is the first time we get here.
                lastMeasuredTrackPosition = trustWorthyPosition;
            }
            else
            {
                // We've already been here...
                if (trustWorthyPosition >= lastMeasuredTrackPosition)
                {
                    // ...and the position has increased, so we're not done yet playing the track.
                    lastMeasuredTrackPosition = trustWorthyPosition;
                    // Nothing to see here, this is not a looping yet...
                }
                else
                {
                    // ...and the position reset, this is an actual loop!
                    CompleteCurrentTrack();
                    msThresholdForNewTrack = 0; // So that we don't risk detecting the new play a second time based on the processed samples count.
                    calculatedPlayedMs = 0; // So that we don' risk scrobbling the track. (see below)
                    TrackStartsPlaying();
                }
            }
        }
    }

    // Check whether the current track can be scrobbled:
    // The track must be longer than 30 seconds.
    // And the track must have been played for at least half its duration, or for 4 minutes(whichever occurs earlier.)
    if (scrobbleCurrentTrackInfoOnEnd == false
        && currentTrackDurationMs > TRACK_DURATION_THRESHOLD_MS
        && (calculatedPlayedMs > (currentTrackDurationMs / 2) || calculatedPlayedMs >= TRACK_PLAY_TIME_THRESHOLD_MS))
    {
        // Do we have enough information to scrobble?
        if (CanScrobble(currentTrackInfo))
        {
            scrobbler->OnTrackCanScrobble(
                currentTrackInfo->artist,
                currentTrackInfo->title,
                currentTrackInfo->album,
                currentTrackDurationMs,
                currentTrackInfo->trackNumber,
                NULL,
                currentTrackInfo->playStartTimestamp);
        }
        scrobbleCurrentTrackInfoOnEnd = true;
    }

    // Keep track of the time the track has played.
    processedSamplesForCurrentTrack += count;
    processedSamplesForCurrentTrackSinceLastReset += count;
    return count;
}

// Complete playing the current track.
// Scrobble it if needed then reset everything for a new track.
static void CompleteCurrentTrack()
{
    if (scrobbleCurrentTrackInfoOnEnd)
    {
        scrobbler->OnTrackCompletes();
        scrobbleCurrentTrackInfoOnEnd = false;
    }
    else if (logCurrentTrackWontScrobbleOnNextTrack)
    {
        SharpScrobblerWrapper::LogInfo("The previous track did not play long enough to be scrobbled.");
    }

    logCurrentTrackWontScrobbleOnNextTrack = false;
    processedSamplesForCurrentTrack = 0;
    processedSamplesForCurrentTrackSinceLastReset = 0;
    lastMeasuredTrackPosition = NAN;
    // Set the expected end of the new track to a temporary safe upper value.
    // (We might not have a track playing yet, so we can't calculate the correct value)
    expectedEndOfCurrentTrackInMs = INT_MAX;
}

// Called when a track starts playing.
// This differs from DSP_NewTrack() in that it correctly accounts for looped tracks.
// (That is, if a track loops, this function is called whereas DSP_NewTrack() is not)
static void TrackStartsPlaying()
{
    currentTrackDurationMs = expectedEndOfCurrentTrackInMs = GetExpectedEndOfCurrentTrackInMs(0);
    scrobbleCurrentTrackInfoOnEnd = false;

    // (Re)initialize currentTrackInfo:
    ReleaseTrackInfo(currentTrackInfo);

    TrackInfo* trackInfo = new TrackInfo();
    trackInfo->playStartTimestamp = time(NULL);
    trackInfo->title = GetTagW(TAG_TITLE);
    trackInfo->artist = GetTagW(TAG_ARTIST);
    trackInfo->album = GetTagW(TAG_ALBUM);
    // If the subsong tag is set, we use it instead of the track tag,
    // because inside a multi-track, the subsong number is more accurate.
    char* subsongNumber = xmpfmisc->GetTag(TAG_SUBSONG); // separated subsong (number/total)
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

    logCurrentTrackWontScrobbleOnNextTrack = true;

    wchar_t* wFilePath = GetStringW(currentFilePath);

    // Do we have enough information to scrobble?
    if (CanScrobble(currentTrackInfo))
    {
        scrobbler->OnTrackStartsPlaying(
            currentTrackInfo->artist,
            currentTrackInfo->title,
            currentTrackInfo->album,
            currentTrackDurationMs,
            currentTrackInfo->trackNumber,
            NULL);

        if (currentTrackDurationMs <= TRACK_DURATION_THRESHOLD_MS)
        {
            SharpScrobblerWrapper::LogWarning(
                (std::wstring(L"Track: '") + NullCheck(currentTrackInfo->title)
                    + L"', artist: '" + NullCheck(currentTrackInfo->artist)
                    + L"', album: '" + NullCheck(currentTrackInfo->album)
                    + L"', from file '" + NullCheck(wFilePath)
                    + L"' is too short (it must be longer than 30 seconds) and will not be scrobbled.").c_str());
            // We don't want to log the same message twice...
            logCurrentTrackWontScrobbleOnNextTrack = false;
        }
    }
    else
    {
        SharpScrobblerWrapper::LogWarning(
            (std::wstring(L"Track: '") + NullCheck(currentTrackInfo->title)
                + L"', artist: '" + NullCheck(currentTrackInfo->artist)
                + L"', album: '" + NullCheck(currentTrackInfo->album)
                + L"', from file '" + NullCheck(wFilePath)
                + L"'is missing mandatory information and will not be scrobbled.").c_str());
        // We don't want to log the same message twice...
        logCurrentTrackWontScrobbleOnNextTrack = false;
    }

    delete[] wFilePath;
}

// Calculate the expected time until the end of the current track from the desired position.
static int GetExpectedEndOfCurrentTrackInMs(int fromPositionMs)
{
    // FIXME: an accurate AND strongly typed duration would be nice...
    char* stringDuration = xmpfmisc->GetTag(TAG_LENGTH);
    double parsed = atof(stringDuration);
    xmpfmisc->Free(stringDuration);
    int currentTrackMaxExpectedDurationMs = (int)(parsed * 1000);
    return currentTrackMaxExpectedDurationMs - fromPositionMs;
}

// Return a value indicating whether the given TrackInfo contains enough information to be scrobbled.
static bool CanScrobble(TrackInfo* trackInfo)
{
    return currentTrackInfo->title != NULL
        && wcslen(currentTrackInfo->title) > 0
        && currentTrackInfo->artist != NULL
        && wcslen(currentTrackInfo->artist) > 0;
}

// Free an instance of TrackInfo.
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

// Get a wide string from an ansi string. (Don't forget to free it)
static wchar_t* GetStringW(const char* string)
{
    if (string != NULL)
    {
        size_t requiredSize = Utf2Uni(string, AUTO_NULL_TERMINATED_LENGTH, NULL, 0);
        wchar_t* buffer = new wchar_t[requiredSize];
        Utf2Uni(string, AUTO_NULL_TERMINATED_LENGTH, buffer, requiredSize);
        return buffer;
    }
    else return NULL;
}

// Get an XMPlay tag as a wide string. (Don't forget to free it)
static wchar_t* GetTagW(const char* tag)
{
    char* value = xmpfmisc->GetTag(tag);
    wchar_t* wValue = NULL;
    if (value != NULL)
        wValue = GetStringW(value);
    xmpfmisc->Free(value);
    return wValue;
}

static std::wstring NullCheck(wchar_t* string)
{
    return string ? std::wstring(string) : std::wstring();
}


// Get the plugin's XMPDSP interface.
XMPDSP* WINAPI XMPDSP_GetInterface2(DWORD face, InterfaceProc faceproc)
{
    if (face != XMPDSP_FACE) return NULL;
    xmpfmisc = (XMPFUNC_MISC*)faceproc(XMPFUNC_MISC_FACE); // Import misc functions.
    xmpfstatus = (XMPFUNC_STATUS*)faceproc(XMPFUNC_STATUS_FACE); // Import playback status functions.
    return &dsp;
}

BOOL WINAPI DllMain(HINSTANCE hDLL, DWORD reason, LPVOID reserved)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls(hDLL);
        hDll = hDLL;
    }
    return 1;
}
