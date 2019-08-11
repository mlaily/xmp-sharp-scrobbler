#pragma once

#include <Windows.h>

extern "C"
{
    __declspec(dllexport) typedef struct {
        void (WINAPI* LogInfo)(LPCWSTR text);
        void (WINAPI* LogWarning)(LPCWSTR text);
        void (WINAPI* LogVerbose)(LPCWSTR text);
        void (WINAPI* Free)();
        LPCSTR(WINAPI* AskUserForNewAuthorizedSessionKey)(HWND win);
        void (WINAPI* SetSessionKey)(LPCSTR text);
        void (WINAPI* OnTrackStartsPlaying)(
            LPCWSTR artist,
            LPCWSTR track,
            LPCWSTR album,
            int duration,
            LPCWSTR trackNumber,
            LPCWSTR mbid);
        void (WINAPI* OnTrackCanScrobble)(
            LPCWSTR artist,
            LPCWSTR track,
            LPCWSTR album,
            int duration,
            LPCWSTR trackNumber,
            LPCWSTR mbid,
            time_t utcUnixTimestamp);
        void (WINAPI* OnTrackCompletes)();
    } ManagedExports;

    __declspec(dllexport) void WINAPI InitializeManagedExports(ManagedExports* exports);
}

extern ManagedExports* pManagedExports;
