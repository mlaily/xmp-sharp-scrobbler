#pragma once

// Required to host the CLR
#include<metahost.h>
#pragma comment(lib, "mscoree.lib")

ICLRRuntimeHost* InitializeCLRRuntimeHost();
DWORD InitializeAssembly(ICLRRuntimeHost* runtimeHost, LPCWSTR assemblyPath, LPCWSTR arg = NULL);
