// Copyright(c) 2019 Melvyn Laïly
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

#pragma once

// Required to host the CLR
#include<metahost.h>
#pragma comment(lib, "mscoree.lib")

// =============================================================================
// APPLICATION SPECIFIC EXPORTS:
// =============================================================================

extern "C"
{
    // Config structure, as stored by XMPlay.
    typedef struct
    {
        byte sessionKey[32];
        byte userName[128];
    } ScrobblerConfig;

    // Native declaration of the managed exported methods.
    __declspec(dllexport) typedef struct {
        void (WINAPI* FreeManagedExports)();
        void (WINAPI* LogInfo)(LPCWSTR text);
        void (WINAPI* LogWarning)(LPCWSTR text);
        void (WINAPI* LogVerbose)(LPCWSTR text);
        ScrobblerConfig* (WINAPI* AskUserForNewAuthorizedSessionKey)(HWND win);
        void (WINAPI* SetSessionKey)(ScrobblerConfig* config);
        void (WINAPI* OnTrackStartsPlaying)(
            LPCWSTR title,
            LPCWSTR artist,
            LPCWSTR album,
            LPCWSTR trackNumber,
            int duration);
        void (WINAPI* OnTrackCanScrobble)(
            LPCWSTR title,
            LPCWSTR artist,
            LPCWSTR album,
            LPCWSTR trackNumber,
            int duration,
            time_t utcUnixTimestamp);
        void (WINAPI* OnTrackCompletes)();
    } ManagedExports;
}

// =============================================================================
// END OF APPLICATION SPECIFIC EXPORTS
// =============================================================================

// Use this after initializing the managed plugin to access its exported methods.
extern ManagedExports* pManagedExports;

// Gets or initalizes the CLR for the current process,
// then executes the method "Plugin.EntryPoint(string arg)" in the provided assembly and return the result of the call.
// Use "pManagedExports" after initializing the managed plugin to access its exported methods. 
DWORD InitializeManagedPlugin(LPCWSTR managedPluginAssemblyPath, LPCWSTR arg = NULL);

// Releases pManagedExports then releases the CLR runtime host used to initialize the managed plugin.
ULONG ReleaseManagedPlugin();

extern "C"
{
    // Callback for the managed plugin.
    // Should be called in its "Plugin.EntryPoint(string arg)" method so that the native code has access to managed methods.
    __declspec(dllexport) void WINAPI InitializeManagedExports(ManagedExports* exports);
}
