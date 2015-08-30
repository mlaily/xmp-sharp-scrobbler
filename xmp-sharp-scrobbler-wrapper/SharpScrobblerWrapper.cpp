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

    public: SharpScrobblerWrapper()
    {
        _private = new SharpScrobblerWrapperPrivate();
        _private->_SharpScrobbler = gcnew SharpScrobbler();
    }

    public: ~SharpScrobblerWrapper()
    {
        delete _private;
    }

    public: static void Initialize()
    {
        SharpScrobbler::Initialize();
    }
};