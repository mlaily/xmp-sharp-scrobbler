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

#include "clr-initializer.h"

ICLRRuntimeHost* InitializeCLRRuntimeHost()
{
	ICLRMetaHost* metaHost = NULL;
	ICLRRuntimeInfo* runtimeInfo = NULL;
	ICLRRuntimeHost* runtimeHost = NULL;
	if (CLRCreateInstance(CLSID_CLRMetaHost, IID_PPV_ARGS(&metaHost)) == S_OK)
		if (metaHost->GetRuntime(L"v4.0.30319", IID_PPV_ARGS(&runtimeInfo)) == S_OK)
			if (runtimeInfo->GetInterface(CLSID_CLRRuntimeHost, IID_PPV_ARGS(&runtimeHost)) == S_OK)
			{
				// Will fail if the CLR is already loaded by another plugin.
				// We should be able to safely ignore this error...
				HRESULT startResult = runtimeHost->Start();

				if (runtimeInfo != NULL)
					runtimeInfo->Release();
				if (metaHost != NULL)
					metaHost->Release();
				return runtimeHost;
			}
	return NULL;
}