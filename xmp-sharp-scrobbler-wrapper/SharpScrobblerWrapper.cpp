#include "Stdafx.h"
#include <windows.h>
#include <msclr\auto_gcroot.h>

#using "xmp-sharp-scrobbler-managed.dll"
#using "System.dll"

using namespace System;
using namespace Runtime::InteropServices;
using namespace xmp_sharp_scrobbler_managed;

// Create a managed proxy function for a native function pointer
// to allow the C# part to call a native function:

static void(WINAPI *_NativeShowInfoBubble)(const char* text, int displayTimeMs);
static void _ShowInfoBubble(String^ text, int displayTimeMs)
{
    const char* nativeText = (const char*)Marshal::StringToHGlobalAnsi(text).ToPointer();
    _NativeShowInfoBubble(nativeText, displayTimeMs);
};

class SharpScrobblerAdapter
{
public:
    msclr::auto_gcroot<SharpScrobbler^> Instance;
    SharpScrobblerAdapter() : Instance(gcnew SharpScrobbler()) {}
};

class __declspec(dllexport) SharpScrobblerWrapper
{
private:
    SharpScrobblerAdapter* _adapter;

public:
    SharpScrobblerWrapper()
    {
        _adapter = new SharpScrobblerAdapter();
    }
    ~SharpScrobblerWrapper()
    {
        delete _adapter;
    }

    static void InitializeShowBubbleInfo(void(WINAPI *showBubbleInfo)(const char* text, int displayTimeMs))
    {
        _NativeShowInfoBubble = showBubbleInfo;
        Util::InitializeShowBubbleInfo(gcnew ShowInfoBubbleHandler(_ShowInfoBubble));
    }

    void SetSessionKey(const char* sessionKey)
    {
        _adapter->Instance->SessionKey = gcnew String(sessionKey);
    }

    void OnTrackStartsPlaying(const wchar_t* artist, const wchar_t* track, const wchar_t* album, int durationMs, const wchar_t* trackNumber, const char* mbid)
    {
        _adapter->Instance->OnTrackStartsPlaying(gcnew String(artist), gcnew String(track), gcnew String(album), durationMs, gcnew String(trackNumber), gcnew String(mbid));
    }

    void OnTrackCanScrobble(const wchar_t* artist, const wchar_t* track, const wchar_t* album, int durationMs, const wchar_t* trackNumber, const char* mbid, time_t utcUnixTimestamp)
    {
        _adapter->Instance->OnTrackCanScrobble(gcnew String(artist), gcnew String(track), gcnew String(album), durationMs, gcnew String(trackNumber), gcnew String(mbid), utcUnixTimestamp);
    }

    const char* AskUserForNewAuthorizedSessionKey(HWND ownerWindowHandle)
    {
        String^ managedResult = _adapter->Instance->AskUserForNewAuthorizedSessionKey(IntPtr(ownerWindowHandle));
        return (const char*)Marshal::StringToHGlobalAnsi(managedResult).ToPointer();
    }
};