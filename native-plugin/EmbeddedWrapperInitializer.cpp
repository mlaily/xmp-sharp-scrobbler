// Copyright(c) 2015 Melvyn Laïly
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
#include <windows.h>
#include <delayimp.h>

#include "resource.h"
#include "AssemblyInitializer.h"

EXTERN_C IMAGE_DOS_HEADER __ImageBase;
// The pseudovariable __ImageBase represents the DOS header of the module, which happens to be what a Win32 module begins with.
// In other words, it's the base address of the module. And the module base address is the same as its HINSTANCE. 
// Only works with a Microsoft Linker. see http://blogs.msdn.com/b/oldnewthing/archive/2004/10/25/247180.aspx
#define HINST_THISCOMPONENT ((HINSTANCE)&__ImageBase)

HMODULE _hEmbeddedWrapper = NULL;

volatile static long _isCriticalSectionInitialized = 0;
static CRITICAL_SECTION _cs;

const LPCSTR tempFileNamesPrefix = "XSS";

// read an embedded resource and load it as a library, then return its module handle
HMODULE ExtractAndLoad(const HMODULE hDll, WORD resourceId)
{
    // based upon and improved from http://blog.syedgakbar.com/2007/11/embedding-dll-and-binary-files-in-the-executable-applications/

    // Get a temporary file name to copy the embedded dll to
    TCHAR tempFileName[MAX_PATH];
    TCHAR tempPathBuffer[MAX_PATH];
    GetTempPath(MAX_PATH, tempPathBuffer);
    GetTempFileName(tempPathBuffer, tempFileNamesPrefix, 0, tempFileName);

    // First find and load the required resource
    HRSRC hResource = FindResource(hDll, MAKEINTRESOURCE(resourceId), "BINARY");
    HGLOBAL hFileResource = LoadResource(hDll, hResource);

    // Now open and map this to a disk file
    LPVOID lpFile = LockResource(hFileResource);
    DWORD dwSize = SizeofResource(hDll, hResource);

    // Open the file and filemap
    HANDLE hFile = CreateFile(tempFileName, GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    HANDLE hFilemap = CreateFileMapping(hFile, NULL, PAGE_READWRITE, 0, dwSize, NULL);
    LPVOID lpBaseAddress = MapViewOfFile(hFilemap, FILE_MAP_WRITE, 0, 0, 0);

    // Write the file
    CopyMemory(lpBaseAddress, lpFile, dwSize);

    // Unmap the file and close the handles
    UnmapViewOfFile(lpBaseAddress);
    CloseHandle(hFilemap);
    CloseHandle(hFile);

    // Load the extracted dll as a library
    HMODULE loadResult = LoadLibrary(tempFileName);

    return loadResult;
}

// Since we cannot delete extracted files as soon as the program exit,
// we do the next best thing: remove them on the next run of the program...
void CleanPreviousExtractions()
{
    try
    {
        TCHAR tempFileName[MAX_PATH];
        TCHAR tempPath[MAX_PATH];
        TCHAR tempPathPattern[MAX_PATH];
        GetTempPath(MAX_PATH, tempPath);

        strcpy_s(tempPathPattern, tempPath);

        strcat_s(tempPathPattern, MAX_PATH, "\\*");

        WIN32_FIND_DATA ffd;
        HANDLE hFind = INVALID_HANDLE_VALUE;

        hFind = FindFirstFile(tempPathPattern, &ffd);
        // loop over all of the files in the temp directory with a name starting with the prefix, and try to remove them.
        do
        {
            if (!(ffd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) && strncmp(ffd.cFileName, tempFileNamesPrefix, strlen(tempFileNamesPrefix)) == 0)
            {
                try
                {
                    // recreate the full path and try to delete the file
                    strcpy_s(tempFileName, tempPath);
                    strcat_s(tempFileName, MAX_PATH, ffd.cFileName);
                    DeleteFile(tempFileName);
                }
                catch (...) {}
            }
        } while (FindNextFile(hFind, &ffd) != 0);
    }
    catch (...) {}
}

// hook into the dll delayed loading process to properly find the extracted xmp-sharp-scrobbler-wrapper.dll
FARPROC WINAPI DliNotifyHook(unsigned dliNotify, PDelayLoadInfo pdli)
{
    if (dliNotify == dliNotePreLoadLibrary)
    {
        if (lstrcmpiA(pdli->szDll, "xmp-sharp-scrobbler-wrapper.dll") == 0)
        {
            // initialize a critical section in a thread safe way
            if (InterlockedIncrement(&_isCriticalSectionInitialized) == 1)
            {
                InitializeCriticalSection(&_cs);
            }
            _isCriticalSectionInitialized = 1; // prevent potential overflow from the increment

            EnterCriticalSection(&_cs);

            // FIXME: maybe add a try catch for the critical section? (not sure since we want it to actually crash if it does not succeeed)

            if (_hEmbeddedWrapper == NULL) // first time the wrapper is required. We have to load it...
            {
                CleanPreviousExtractions();
                _hEmbeddedWrapper = ExtractAndLoad(HINST_THISCOMPONENT, EMBEDDED_WRAPPER_RESOURCE_ID);
                // now that we have extracted the wrapper, initialize it!
                // (this will trigger the dll delayed loading hook since this is the first time we use a method from the wrapper)
                InitializeManagedWrapper();
            }

            LeaveCriticalSection(&_cs);

            return (FARPROC)_hEmbeddedWrapper;
        }
    }
    return NULL;
}
// https://msdn.microsoft.com/en-us/library/z9h1h6ty.aspx
extern "C" const PfnDliHook __pfnDliNotifyHook2 = DliNotifyHook;
