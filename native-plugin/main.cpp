#include <iostream>
#include <windows.h>

#include "xmpdsp.h"
#include "xmpfunc.h"

#include "YahooAPIWrapper.h"

static XMPFUNC_MISC* xmpfmisc;
static XMPFUNC_STATUS* xmpfstatus;

static HINSTANCE ghInstance;

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

/* DSP functions: */

static void WINAPI DSP_About(HWND win);
static void* WINAPI DSP_New(void);
static void WINAPI DSP_Free(void* inst);
static const char* WINAPI DSP_GetDescription(void* inst);
static void WINAPI DSP_Config(void* inst, HWND win);
static DWORD WINAPI DSP_GetConfig(void* inst, void* config);
static BOOL WINAPI DSP_SetConfig(void* inst, void* config, DWORD size);
static void WINAPI DSP_NewTrack(void* inst, const char* file);
static void WINAPI DSP_SetFormat(void* inst, const XMPFORMAT* form);
static void WINAPI DSP_Reset(void* inst);
static DWORD WINAPI DSP_Process(void* inst, float* data, DWORD count);
static void WINAPI DSP_NewTitle(void* inst, const char* title);

/* My functions: */

static void TrackStartsPlaying();
static void ResetForNewTrack();
static void SetExpectedEndOfCurrentTrackInMs(int fromPositionMs);

// config structure
typedef struct
{
    BOOL showCues;
    BOOL keepOnClose;
} PluginConfig;
static PluginConfig pluginConf;

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

void hello()
{
    const char* stock = "GOOG";
    YahooAPIWrapper yahoo;

    double bid = yahoo.GetBid(stock);
    double ask = yahoo.GetAsk(stock);
    const char* capi = yahoo.GetCapitalization("éµ");

    const char** bidAskCapi = yahoo.GetValues(stock, "b3b2j1");
}

static void WINAPI DSP_About(HWND win)
{
    //hello();
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
    return (void*)1;
}

static void WINAPI DSP_Free(void* inst)
{
}

static void WINAPI DSP_Config(void* inst, HWND win)
{
}

static DWORD WINAPI DSP_GetConfig(void* inst, void* config)
{
    memcpy(config, &pluginConf, sizeof(pluginConf));
    return sizeof(pluginConf); // return size of config info
}

static BOOL WINAPI DSP_SetConfig(void* inst, void* config, DWORD size)
{
    memcpy(&pluginConf, config, min(size, sizeof(pluginConf)));
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
    if (form != NULL)
    {
        xmprateBy1000 = form->rate / 1000;
    }
    else
    {
        xmprateBy1000 = 0;
    }
    ResetForNewTrack();
}

// This is apparently useless as I have never seen it called once.
static void WINAPI DSP_NewTitle(void* inst, const char* title) { }

// Called when the user seeks into the track.
static void WINAPI DSP_Reset(void* inst)
{
    double resetPosition = xmpfstatus->GetTime();
    SetExpectedEndOfCurrentTrackInMs((int)resetPosition * 1000);
    processedSamplesForCurrentTrackSinceLastReset = 0;
}

static DWORD WINAPI DSP_Process(void* inst, float* data, DWORD count)
{
    // the following code checks whether the processed track is a track just starting to play or not

    if (processedSamplesForCurrentTrack == 0) msThresholdForNewTrack = count / xmprateBy1000;
    int calculatedPlayedMs = processedSamplesForCurrentTrack / xmprateBy1000;
    if (calculatedPlayedMs < msThresholdForNewTrack)
    {
        // new track starts playing normally
        // take into account the currently processed data.
        // we don't use xmpfstatus->GetTime() because it's not perfectly synchronized with the actual position.
        SetExpectedEndOfCurrentTrackInMs(0);
        TrackStartsPlaying();
    }
    else
    {
        // in the middle of a track. Before continuing, we check whether the track has looped without XMPlay telling us
        int calculatedPlayedMsSinceLastReset = processedSamplesForCurrentTrackSinceLastReset / xmprateBy1000;
        if (calculatedPlayedMsSinceLastReset > expectedEndOfCurrentTrackInMs)
        {
            // the calculated play time, based on the number of samples actually played,
            // is superior to the length of the track, but XMPlay did not signal a new track.
            // this means the track was looped, so this is effectively a new play.
            // so we reset everything like if it were a new track:
            ResetForNewTrack();
            // so that we don't risk detecting the new play a second time based on the processed samples count
            msThresholdForNewTrack = 0;
            // take into account the currently processed data.
            // we don't use xmpfstatus->GetTime() because it's not perfectly synchronized with the actual position.
            SetExpectedEndOfCurrentTrackInMs(0);
            TrackStartsPlaying();
        }
    }
    processedSamplesForCurrentTrack += count;
    processedSamplesForCurrentTrackSinceLastReset += count;
    return count;
}

// Prepare everything for a new current track.
// (current number of processed samples, expected end of track...)
static void ResetForNewTrack()
{
    processedSamplesForCurrentTrack = 0;
    processedSamplesForCurrentTrackSinceLastReset = 0;
    // temporary safe upper value
    // (we might not have a track playing yet, so we can't calculate the correct value)
    expectedEndOfCurrentTrackInMs = INT_MAX;
}

// Calculate the expected time until the end of the current track from the desired position,
// and set it to expectedEndOfCurrentTrackInMs.
static void SetExpectedEndOfCurrentTrackInMs(int fromPositionMs)
{
    // use this instead of GetTag(TAG_LENGTH) so that we have an int directly
    int currentTrackLengthSeconds = SendMessage(xmpfmisc->GetWindow(), WM_WA_IPC, 1, IPC_GETOUTPUTTIME);
    // this is the truncated duration in ms + 1sec to account for the loss of precision
    // so that we don't signal a new track playing before it's actually looped
    int currentTrackMaxExpectedDurationMs = currentTrackLengthSeconds * 1000 + 1000;
    expectedEndOfCurrentTrackInMs = currentTrackMaxExpectedDurationMs - fromPositionMs;
}

// Called when a track starts playing.
// This differs from DSP_NewTrack() in that it correctly accounts for looped tracks.
// (That is, if a track loops, this function is called whereas DSP_NewTrack() is not)
static void TrackStartsPlaying()
{
    char* general = xmpfmisc->GetInfoText(XMPINFO_TEXT_GENERAL);
    char* message = xmpfmisc->GetInfoText(XMPINFO_TEXT_MESSAGE);
    char* samples = xmpfmisc->GetInfoText(XMPINFO_TEXT_SAMPLES);

    char* formattedTrackTitle = xmpfmisc->GetTag(TAG_FORMATTED_TITLE); // formatted track title
    char* filename = xmpfmisc->GetTag(TAG_FILENAME); // filename
    char* trackTitle = xmpfmisc->GetTag(TAG_TRACK_TITLE);// stream track (or CUE sheet) title
    char* lengthSeconds = xmpfmisc->GetTag(TAG_LENGTH);// length in seconds
    char* subsongCount = xmpfmisc->GetTag(TAG_SUBSONGS);// subsong count
    char* subsongNumber = xmpfmisc->GetTag(TAG_SUBSONG);// separated subsong (number/total)
    char* rating = xmpfmisc->GetTag(TAG_RATING);// user rating
    char* title = xmpfmisc->GetTag(TAG_TITLE);// = "title"
    char* artist = xmpfmisc->GetTag(TAG_ARTIST);// = "artist"
    char* album = xmpfmisc->GetTag(TAG_ALBUM); // = "album"
    char* date = xmpfmisc->GetTag(TAG_DATE); // = "date"
    char* trackNumber = xmpfmisc->GetTag(TAG_TRACK);// = "track"
    char* genre = xmpfmisc->GetTag(TAG_GENRE); // = "genre"
    char* comment = xmpfmisc->GetTag(TAG_COMMENT); // = "comment"
    char* filetype = xmpfmisc->GetTag(TAG_FILETYPE);// = "filetype"

    const XMPFORMAT* inFormat = xmpfstatus->GetFormat(TRUE);
    const XMPFORMAT* outFormat = xmpfstatus->GetFormat(FALSE);

    DWORD latency = xmpfstatus->GetLatency();
    double time = xmpfstatus->GetTime();
    QWORD written = xmpfstatus->GetWritten();
    BOOL isPlaying = xmpfstatus->IsPlaying();
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
        ghInstance = hDLL;
        DisableThreadLibraryCalls(hDLL);
    }
    return 1;
}
