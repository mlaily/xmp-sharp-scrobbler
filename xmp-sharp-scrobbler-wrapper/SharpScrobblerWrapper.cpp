#include "Stdafx.h"

#include <msclr\auto_gcroot.h>

#using "xmp-sharp-scrobbler-managed.dll"
#using "System.dll"

using namespace System;

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

    void SetSessionKey(const char* sessionKey)
    {
        _adapter->Instance->SessionKey = gcnew String(sessionKey);
    }

    void NowPlaying(const wchar_t* artist, const wchar_t* track, const wchar_t* album, int durationMs, const wchar_t* trackNumber, const char* mbid)
    {
        _adapter->Instance->NowPlaying(gcnew String(artist), gcnew String(track), gcnew String(album), durationMs, gcnew String(trackNumber), gcnew String(mbid));
    }

    void Scrobble(const wchar_t* artist, const wchar_t* track, const wchar_t* album, int durationMs, const wchar_t* trackNumber, const char* mbid, time_t utcUnixTimestamp)
    {
        _adapter->Instance->Scrobble(gcnew String(artist), gcnew String(track), gcnew String(album), durationMs, gcnew String(trackNumber), gcnew String(mbid), utcUnixTimestamp);
    }

    static void Initialize()
    {
        SharpScrobbler::Initialize();
    }

    static const char* AskUserForNewAuthorizedSessionKey()
    {
        String^ managedResult = SharpScrobbler::AskUserForNewAuthorizedSessionKey();

        return (const char*)Runtime::InteropServices::Marshal::StringToHGlobalAnsi(managedResult).ToPointer();
    }
};