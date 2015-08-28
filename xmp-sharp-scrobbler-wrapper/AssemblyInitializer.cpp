#include "Stdafx.h"

System::Reflection::Assembly^ currentDomain_AssemblyResolve(System::Object^ sender, System::ResolveEventArgs^ args)
{
    // http://stackoverflow.com/questions/7016663/loading-mixed-mode-c-cli-dll-and-dependencies-dynamically-from-unmanaged-c

    // If this is an mscorlib, do a bare load
    if (args->Name->Length >= 8 && args->Name->Substring(0, 8) == L"mscorlib")
    {
        return System::Reflection::Assembly::Load(args->Name->Substring(0, args->Name->IndexOf(L",")) + L".dll");
    }

    // Load the assembly from the sub-directory
    System::String^ finalPath = nullptr;
    try
    {
        System::IO::Stream^ stream = System::Reflection::Assembly::GetExecutingAssembly()->GetManifestResourceStream("xmp-sharp-scrobbler-managed.dll");
        array<System::Byte>^ buffer = gcnew array<System::Byte>(stream->Length);
        stream->Read(buffer, 0, stream->Length);

        //finalPath = gcnew System::String("xmp-sharp-scrobbler/") + args->Name->Substring(0, args->Name->IndexOf(",")) + ".dll";
        System::Reflection::Assembly^ retval = System::Reflection::Assembly::Load(buffer);
        return retval;
    }
    catch (...)
    {
    }

    return nullptr;
}

__declspec(dllexport) void InitializeManagedWrapper(void)
{
    // Set up our resolver for assembly loading
    System::AppDomain^ currentDomain = System::AppDomain::CurrentDomain;
    currentDomain->AssemblyResolve += gcnew System::ResolveEventHandler(currentDomain_AssemblyResolve);
}