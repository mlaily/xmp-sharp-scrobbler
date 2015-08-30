#include "Stdafx.h"

#include <msclr\auto_gcroot.h>

#using "xmp-sharp-scrobbler-managed.dll"

using namespace System::Runtime::InteropServices; // Marshal

class __declspec(dllexport) ScrobblerWrapper
{
    public: static void Initialize()
    {
        Scrobbler::Initialize();
    }
};