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

    void NowPlaying(const char* artist, const char* track, const char* album, int durationMs, const char* trackNumber, const char* mbid)
    {
        _adapter->Instance->NowPlaying(gcnew String(artist), gcnew String(track), gcnew String(album), durationMs, gcnew String(trackNumber), gcnew String(mbid));
    }

    void Scrobble(const char* artist, const char* track, const char* album, int durationMs, int playTimeBeforeScrobbleMs, const char* trackNumber, const char* mbid)
    {
        _adapter->Instance->Scrobble(gcnew String(artist), gcnew String(track), gcnew String(album), durationMs, playTimeBeforeScrobbleMs, gcnew String(trackNumber), gcnew String(mbid));
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