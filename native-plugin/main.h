#pragma once

#include "time.h"

#define PLUGIN_VERSION          0,1,0,0
#define PLUGIN_VERSION_STRING   "0.1.0.0"

// config structure
typedef struct
{
    char sessionKey[32];
} ScrobblerConfig;

typedef struct
{
    time_t playStartTimestamp;
    wchar_t* title;
    wchar_t* artist;
    wchar_t* album;
    wchar_t* trackNumber;
} TrackInfo;

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

/* Plugin functions: */

static void ResetForNewTrack();
static int GetExpectedEndOfCurrentTrackInMs(int fromPositionMs);
static void TrackStartsPlaying();
static void ScrobbleTrack();
static void InitializeCurrentTrackInfo();
static void ReleaseTrackInfo(TrackInfo* trackInfo);