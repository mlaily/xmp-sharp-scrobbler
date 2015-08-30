class SharpScrobblerWrapperPrivate;

class SharpScrobblerWrapper
{
    private: SharpScrobblerWrapperPrivate* _private;

    public: SharpScrobblerWrapper(const char* sessionKey);
    public: ~SharpScrobblerWrapper();

    public: static void Initialize();
    public: static const char* AskUserForNewAuthorizedSessionKey();
};
