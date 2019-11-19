#pragma once

// WARNING: special characters will be mangled if this file does not stay encoded as UTF-8-BOM

#include "time.h"

#define PLUGIN_FRIENDLY_NAME    "XMPlay Sharp Scrobbler"
#define PLUGIN_VERSION          0,6,0,0
#define PLUGIN_VERSION_STRING   "0.6.0.0"
#define IDD_ABOUT               1001
#define IDC_ABOUT_DOTNET_LINK   1002
#define ABOUT_DIALOG_TEXT PLUGIN_FRIENDLY_NAME "\n\nA Last.fm scrobbling plugin for XMPlay.\n\nVersion " PLUGIN_VERSION_STRING \
" - 2019\n\nBy Melvyn Laïly\n\nThis plugin requires the .Net Framework 4.6 to be installed to run.\n\n<a>Download .Net 4.6</a>"


// Config structure, as stored by XMPlay.
typedef struct
{
    char sessionKey[32];
} ScrobblerConfig;

// Gather information required to scrobble a track.
// All the text fields are expected to be UTF-16.
typedef struct
{
    time_t playStartTimestamp;
    wchar_t* title;
    wchar_t* artist;
    wchar_t* album;
    wchar_t* trackNumber;
    wchar_t* mbid;
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
static wchar_t* GetStringW(const char* string);
static std::string utf8_encode(const std::wstring& wstr);
static std::wstring utf8_decode(const std::string& str);
static wchar_t* GetTagW(const char* tag);
static std::wstring NullCheck(wchar_t* string);

static BOOL CALLBACK AboutDialogProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

/* Exported functions: */
extern "C" {
    __declspec(dllexport) void WINAPI ShowInfoBubble(LPCWSTR text, int displayTimeMs);
}
