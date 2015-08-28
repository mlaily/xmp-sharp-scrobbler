#pragma once

#include <windows.h>

#define EMBEDDED_WRAPPER_RESOURCE_ID 1001

// Provided the current module handle, load the embedded
// xmp-sharp-scrobbler-managed.dll to be used by the current process
void InitializeEmbeddedManagedWrapper(HMODULE hDll);