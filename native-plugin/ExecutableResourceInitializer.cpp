#include <iostream>
#include <windows.h>
#include <delayimp.h>

#include "EmbeddedWrapperInitializer.h"
#include "AssemblyInitializer.h"

HMODULE _hEmbeddedWrapper = NULL;

// hook into the dll delayed loading process to properly find the extracted xmp-sharp-scrobbler-wrapper.dll
FARPROC WINAPI DliNotifyHook(unsigned dliNotify, PDelayLoadInfo pdli)
{
    if (dliNotify == dliNotePreLoadLibrary)
    {
        if (lstrcmpiA(pdli->szDll, "xmp-sharp-scrobbler-wrapper.dll") == 0)
        {
            if (_hEmbeddedWrapper == NULL)
            {
                throw std::runtime_error("DliNotifyHook: _hEmbeddedWrapper is NULL! Did you forget to call InitializeEmbeddedManagedWrapper()?");
            }
            else
            {
                return (FARPROC)_hEmbeddedWrapper;
            }
        }
    }
    return NULL;
}
extern "C" PfnDliHook __pfnDliNotifyHook2 = DliNotifyHook;

// read an embedded resource and load it as a library, then return its module handle
HMODULE ExtractAndLoad(const HMODULE hDll, WORD resourceId)
{
    // based upon and improved from http://blog.syedgakbar.com/2007/11/embedding-dll-and-binary-files-in-the-executable-applications/

    // Get a temporary file name to copy the embedded dll to
    TCHAR szTempFileName[MAX_PATH];
    TCHAR lpTempPathBuffer[MAX_PATH];
    GetTempPath(MAX_PATH, lpTempPathBuffer);
    GetTempFileName(lpTempPathBuffer, "XSS", 0, szTempFileName);

    // First find and load the required resource
    HRSRC hResource = FindResource(hDll, MAKEINTRESOURCE(resourceId), "BINARY");
    HGLOBAL hFileResource = LoadResource(hDll, hResource);

    // Now open and map this to a disk file
    LPVOID lpFile = LockResource(hFileResource);
    DWORD dwSize = SizeofResource(hDll, hResource);

    // Open the file and filemap
    HANDLE hFile = CreateFile(szTempFileName, GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    HANDLE hFilemap = CreateFileMapping(hFile, NULL, PAGE_READWRITE, 0, dwSize, NULL);
    LPVOID lpBaseAddress = MapViewOfFile(hFilemap, FILE_MAP_WRITE, 0, 0, 0);

    // Write the file
    CopyMemory(lpBaseAddress, lpFile, dwSize);

    // Unmap the file and close the handles
    UnmapViewOfFile(lpBaseAddress);
    CloseHandle(hFilemap);
    CloseHandle(hFile);

    // Load the extracted dll as a library
    HMODULE loadResult = LoadLibrary(szTempFileName);

    // Mark the file for deletion once all handles (the one from LoadLibrary) are closed
    DeleteFile(szTempFileName);

    return loadResult;
}

void InitializeEmbeddedManagedWrapper(HMODULE hDll)
{
    _hEmbeddedWrapper = ExtractAndLoad(hDll, EMBEDDED_WRAPPER_RESOURCE_ID);
    // now that we have extracted the wrapper, initialize it!
    // (this will trigger the dll delayed loading hook since this is the first time we use a method from the wrapper)
    InitializeManagedWrapper();
}