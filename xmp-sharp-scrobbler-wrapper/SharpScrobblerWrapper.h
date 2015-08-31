class SharpScrobblerAdapter;

class SharpScrobblerWrapper
{
private:
    SharpScrobblerAdapter* _adapter;

public:
    SharpScrobblerWrapper();
    ~SharpScrobblerWrapper();

    void SetSessionKey(const char* sessionKey);

    void NowPlaying(const char* artist, const char* track, const char* album, int durationMs, const char* trackNumber, const char* mbid);
    void Scrobble(const char* artist, const char* track, const char* album, int durationMs, int playTimeBeforeScrobbleMs, const char* trackNumber, const char* mbid);

    static void Initialize();
    static const char* AskUserForNewAuthorizedSessionKey();
};
