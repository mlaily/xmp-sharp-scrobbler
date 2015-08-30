#include "Stdafx.h"

#include <msclr\auto_gcroot.h>

#using "xmp-sharp-scrobbler-managed.dll"

using namespace System::Runtime::InteropServices; // Marshal

class SharpScrobblerWrapperPrivate
{
    public: msclr::auto_gcroot<SharpScrobbler^> _SharpScrobbler;
};

class __declspec(dllexport) SharpScrobblerWrapper
{
    private: SharpScrobblerWrapperPrivate* _private;

    public: SharpScrobblerWrapper(const char* sessionKey)
    {
        _private = new SharpScrobblerWrapperPrivate();
        _private->_SharpScrobbler = gcnew SharpScrobbler(gcnew System::String(sessionKey));
    }

    public: ~SharpScrobblerWrapper()
    {
        delete _private;
    }

    public: static void Initialize()
    {
        SharpScrobbler::Initialize();
    }

    public: static const char* AskUserForNewAuthorizedSessionKey()
    {
        System::String^ managedResult = SharpScrobbler::AskUserForNewAuthorizedSessionKey();

        return (const char*)Marshal::StringToHGlobalAnsi(managedResult).ToPointer();
    }
};