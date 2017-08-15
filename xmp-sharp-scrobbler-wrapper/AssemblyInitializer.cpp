#include "Stdafx.h"

System::Reflection::Assembly^ currentDomain_AssemblyResolve(System::Object^ sender, System::ResolveEventArgs^ args)
{
    // http://stackoverflow.com/questions/7016663/loading-mixed-mode-c-cli-dll-and-dependencies-dynamically-from-unmanaged-c

    System::Reflection::AssemblyName^ assemblyName = gcnew System::Reflection::AssemblyName(args->Name);

    if (assemblyName->Name == L"xmp-sharp-scrobbler-managed")
    {
        try
        {
            System::IO::Stream^ stream = System::Reflection::Assembly::GetExecutingAssembly()->GetManifestResourceStream("xmp-sharp-scrobbler-managed.dll");
            array<System::Byte>^ buffer = gcnew array<System::Byte>((int)(stream->Length));
            stream->Read(buffer, 0, (int)(stream->Length));

            System::Reflection::Assembly^ retval = System::Reflection::Assembly::Load(buffer);
            return retval;
        }
        catch (...)
        {
        }
    }

    return nullptr;
}

__declspec(dllexport) void InitializeManagedWrapper(void)
{
    // Set up our resolver for assembly loading
    System::AppDomain^ currentDomain = System::AppDomain::CurrentDomain;
    currentDomain->AssemblyResolve += gcnew System::ResolveEventHandler(currentDomain_AssemblyResolve);
}