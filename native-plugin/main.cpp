/*
  XMPlay MSN Plugin (c) 2005-2006 Elliott Sales de Andrade

Copyright (c) 2005-2006 Elliott Sales de Andrade

This software is provided 'as-is', without any express or implied warranty.
In no event will the authors be held liable for any damages arising from the
use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it freely,
subject to the following restrictions:

    1. The origin of this software must not be misrepresented; you must not
       claim that you wrote the original software. If you use this software
       in a product, an acknowledgment in the product documentation would be
       appreciated but is not required.

    2. Altered source versions must be plainly marked as such, and must not
       be misrepresented as being the original software.

    3. This notice may not be removed or altered from any source distribution.

Update 7
  - set XMPDSP_FLAG_NODSP flag (for "general" plugin)
Update 6
  - cue/stream titles
Update 5
  - XMPlay 3.4 support
Update 4, Elliott Sales de Andrade
  - Fall back to ANSI processing on Windows 9x/ME
Update 3, Elliott Sales de Andrade
  - should fix Unicode characters
  - fixed buffer overflow
  - no more major warnings
Update 2, Elliott Sales de Andrade
  - Incorporate Svante's changes to my copy
  - Stop showing "XMPlay 3.2"
  - Mini config
  - Source rearrangements
Update 1, Svante Boberg
  - Fixed crash when trying to open configuration
  - Safer release of subclass, sometimes crashed the player in last version
  - Automatically sets MSN now playing when loading plugin
*/

#include <iostream>
#include <windows.h>

#include "xmpdsp.h" // requires the XMPlay "DSP/general plugin SDK"

#include "YahooAPIWrapper.h"

static XMPFUNC_MISC *xmpfmisc;

static HINSTANCE ghInstance;
static BOOL isUnicode;

static HWND xmpwin;
static HHOOK hook;

static BOOL CALLBACK DSPDialogProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

static void WINAPI DSP_About(HWND win);
static void *WINAPI DSP_New(void);
static void WINAPI DSP_Free(void *inst);
static const char *WINAPI DSP_GetDescription(void *inst);
static void WINAPI DSP_Config(void *inst, HWND win);
static DWORD WINAPI DSP_GetConfig(void *inst, void *config);
static BOOL WINAPI DSP_SetConfig(void *inst, void *config, DWORD size);

// config structure
typedef struct {
    BOOL showCues;
    BOOL keepOnClose;
} MSNStuff;
static MSNStuff msnConf;

static XMPDSP dsp = {
    XMPDSP_FLAG_NODSP,
    "XMPlay2MSN",
    DSP_About,
    DSP_New,
    DSP_Free,
    DSP_GetDescription,
    DSP_Config,
    DSP_GetConfig,
    DSP_SetConfig,
};

void hello()
{
    const char* stock = "GOOG";
    YahooAPIWrapper yahoo;

    double bid = yahoo.GetBid(stock);
    double ask = yahoo.GetAsk(stock);
    const char* capi = yahoo.GetCapitalization("éµ");

    const char** bidAskCapi = yahoo.GetValues(stock, "b3b2j1");

    std::cout << "Bid: " << bid << std::endl;
    std::cout << "Ask: " << ask << std::endl;
    std::cout << "Capi: " << capi << std::endl;

    std::cout << "BidAskCapi[0]: " << bidAskCapi[0] << std::endl;
    std::cout << "BidAskCapi[1]: " << bidAskCapi[1] << std::endl;
    std::cout << "BidAskCapi[2]: " << bidAskCapi[2] << std::endl;
}

static void WINAPI SetNowPlaying(BOOL close)
{
	COPYDATASTRUCT msndata;
    int strLen;
    wchar_t *lpMsn;
    HWND msnui=0;
	char *title=NULL;
	if (!close) {
		//if (msnConf.showCues) title=xmpfmisc->GetTag(-3); // get cue title
		//if (!title) title=xmpfmisc->GetTag(-1); // get track title
	}
    if (!title) {
		if (msnConf.keepOnClose) return;
        lpMsn = L"\\0Music\\00\\0{0}\\0\\0";
        strLen = 20;
    } else {
		lpMsn = (wchar_t*)xmpfmisc->Alloc(1024);
        // stuff for MSN before...
        memcpy(lpMsn, L"\\0Music\\01\\0{0}\\0", 17*2);
        // actual title...
		strLen=MultiByteToWideChar(isUnicode?CP_UTF8:CP_ACP,0,title,-1,lpMsn+17,492)-1;  /* 1024/2 - 20 */
        // stuff for MSN after...
        memcpy(lpMsn + 17 + strLen, L"\\0", 3*2);
        strLen += 20;
    }
    msndata.dwData = 0x547;
    msndata.lpData = (void*)lpMsn;
    msndata.cbData = strLen * 2;

    while (msnui = FindWindowEx(NULL, msnui, "MsnMsgrUIManager", NULL))
        SendMessage(msnui, WM_COPYDATA, (WPARAM)xmpwin, (LPARAM)&msndata);

    if (title) {
		xmpfmisc->Free(title);
		xmpfmisc->Free(lpMsn);
    }
}

static LRESULT CALLBACK HookProc(int n, WPARAM w, LPARAM l)
{
	if (n==HC_ACTION) {
		CWPSTRUCT *cwp=(CWPSTRUCT*)l;
		if (cwp->message==WM_SETTEXT && cwp->hwnd==xmpwin) // title change
			SetNowPlaying(FALSE);
	}
	return CallNextHookEx(hook,n,w,l);
}

static void WINAPI DSP_About(HWND win)
{
    hello();
	MessageBox(win,
		"XMPlay éµ to MSN-Now-Playing Plugin\nCopyright 2005 Elliott Sales de Andrade"
		"\n\nContributors:\nSvante Boberg\nIan Luck",
		"XMPlay2MSN (rev.7)",
		MB_ICONINFORMATION);
}

static const char *WINAPI DSP_GetDescription(void *inst)
{
    return dsp.name;
}

static void *WINAPI DSP_New()
{
	xmpwin=xmpfmisc->GetWindow();
    msnConf.showCues = TRUE;
    msnConf.keepOnClose = FALSE;

	SetNowPlaying(FALSE);

	// setup hook to catch title changes
	hook=SetWindowsHookEx(WH_CALLWNDPROC,&HookProc,NULL,GetWindowThreadProcessId(xmpwin,NULL));

    return (void*)1;
}

static void WINAPI DSP_Free(void *inst)
{
	UnhookWindowsHookEx(hook);
	SetNowPlaying(TRUE);
}

static void WINAPI DSP_Config(void *inst, HWND win)
{
    DialogBox(ghInstance, MAKEINTRESOURCE(1000), win, &DSPDialogProc);
}

static DWORD WINAPI DSP_GetConfig(void *inst, void *config)
{
    memcpy(config, &msnConf, sizeof(msnConf));
    return sizeof(msnConf); // return size of config info
}

static BOOL WINAPI DSP_SetConfig(void *inst, void *config, DWORD size)
{
    memcpy(&msnConf, config, min(size,sizeof(msnConf)));
	SetNowPlaying(FALSE);
    return TRUE;
}

#define MESS(id,m,w,l) SendDlgItemMessage(hWnd,id,m,(WPARAM)(w),(LPARAM)(l))

static BOOL CALLBACK DSPDialogProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
	switch (msg) {
		case WM_COMMAND:
			switch (LOWORD(wParam)) {
				case IDOK:
					msnConf.showCues = (BST_CHECKED==MESS(10, BM_GETCHECK, 0, 0));
					msnConf.keepOnClose = (BST_CHECKED==MESS(20, BM_GETCHECK, 0, 0));
					SetNowPlaying(FALSE);
				case IDCANCEL:
					EndDialog(hWnd, 0);
					break;
			}
			break;
        case WM_INITDIALOG:
			if (xmpfmisc->GetVersion()>=0x03040100) // check for 3.4.1
				MESS(10, BM_SETCHECK, msnConf.showCues?BST_CHECKED:BST_UNCHECKED, 0);
			else
				EnableWindow(GetDlgItem(hWnd,10),FALSE);
			MESS(20, BM_SETCHECK, msnConf.keepOnClose?BST_CHECKED:BST_UNCHECKED, 0);
			return TRUE;
    }
	return FALSE;
}

// get the plugin's XMPDSP interface
XMPDSP *WINAPI XMPDSP_GetInterface2(DWORD face, InterfaceProc faceproc)
{
	if (face!=XMPDSP_FACE) return NULL;
	xmpfmisc=(XMPFUNC_MISC*)faceproc(XMPFUNC_MISC_FACE); // import "misc" functions
	return &dsp;
}

BOOL WINAPI DllMain(HINSTANCE hDLL, DWORD reason, LPVOID reserved)
{
    if (reason==DLL_PROCESS_ATTACH) {
        ghInstance=hDLL;
        DisableThreadLibraryCalls(hDLL);
		isUnicode=true;
    }
    return 1;
}
