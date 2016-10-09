class SharpScrobblerAdapter;

class SharpScrobblerWrapper
{
private:
    SharpScrobblerAdapter* _adapter;

public:
    SharpScrobblerWrapper();
    ~SharpScrobblerWrapper();

    static void InitializeShowBubbleInfo(void(WINAPI *showBubbleInfo)(const char* text, int displayTimeMs));

    static void LogMessage(const char* message);
    static void LogMessage(const wchar_t* message);

    void SetSessionKey(const char* sessionKey);

    void OnTrackStartsPlaying(const wchar_t* artist, const wchar_t* track, const wchar_t* album, int durationMs, const wchar_t* trackNumber, const char* mbid);
    void OnTrackCanScrobble(const wchar_t* artist, const wchar_t* track, const wchar_t* album, int durationMs, const wchar_t* trackNumber, const char* mbid, time_t utcUnixTimestamp);
    void OnTrackCompletes();

    const char* AskUserForNewAuthorizedSessionKey(HWND ownerWindowHandle);
};
