#pragma once

// WARNING: special characters will be mangled if this file does not stay encoded as UTF-8-BOM

#include "time.h"

#define PLUGIN_FRIENDLY_NAME    "XMPlay Sharp Scrobbler"
#define PLUGIN_VERSION          0,6,1,0
#define PLUGIN_VERSION_STRING   "0.6.1.0"
#define PLUGIN_COPYRIGHT_STRING "Copyright (C) Melvyn Laïly 2016-2020"
#define IDD_ABOUT               1001
#define IDC_ABOUT_DOTNET_LINK   1002
#define ABOUT_DIALOG_TEXT PLUGIN_FRIENDLY_NAME "\n\nA Last.fm scrobbling plugin for XMPlay.\n\nVersion " PLUGIN_VERSION_STRING "\n" \
PLUGIN_COPYRIGHT_STRING "\n\nThis plugin requires the .NET Framework 4.6 or superior.\n\n<a>Download .NET 4.6</a>"

// Gather information required to scrobble a track.
// All the text fields are expected to be UTF-16.
typedef struct
{
    time_t playStartTimestamp;
    LPCWSTR title;
    LPCWSTR artist;
    LPCWSTR album;
    LPCWSTR trackNumber;
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

static void CompleteCurrentTrack();
static void InitializeCurrentTrackInfo();
static void TrackStartsPlaying();
static bool CanScrobble(TrackInfo* trackInfo);
static void FreeTrackInfo(TrackInfo* trackInfo);
static int GetExpectedEndOfCurrentTrackInMs(int fromPositionMs);
static LPCWSTR GetStringW(const char* string);
static std::string utf8_encode(const std::wstring& wstr);
static std::wstring utf8_decode(const std::string& str);
static LPCWSTR GetTagW(const char* tag);
static std::wstring NullCheck(LPCWSTR string);

static BOOL CALLBACK AboutDialogProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

/* Exported functions: */
extern "C" {
    __declspec(dllexport) void WINAPI ShowInfoBubble(LPCWSTR text, int displayTimeMs);
}
