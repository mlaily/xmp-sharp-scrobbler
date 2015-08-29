#include <iostream>
#include <windows.h>

#include "xmpdsp.h"

#include "YahooAPIWrapper.h"

static XMPFUNC_MISC *xmpfmisc;

static HINSTANCE ghInstance;

static void WINAPI DSP_About(HWND win);
static void *WINAPI DSP_New(void);
static void WINAPI DSP_Free(void *inst);
static const char *WINAPI DSP_GetDescription(void *inst);
static void WINAPI DSP_Config(void *inst, HWND win);
static DWORD WINAPI DSP_GetConfig(void *inst, void *config);
static BOOL WINAPI DSP_SetConfig(void *inst, void *config, DWORD size);

// config structure
typedef struct
{
    BOOL showCues;
    BOOL keepOnClose;
} PluginConfig;
static PluginConfig pluginConf;

static XMPDSP dsp =
{
    XMPDSP_FLAG_NODSP,
    "XMPlay Sharp Scrobbler",
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
}

static void WINAPI DSP_About(HWND win)
{
    hello();
    MessageBox(win,
        "XMPlay éµ\n",
        "XMPlay Sharp Scrobbler",
        MB_ICONINFORMATION);
}

static const char *WINAPI DSP_GetDescription(void *inst)
{
    return dsp.name;
}

static void *WINAPI DSP_New()
{
    return (void*)1;
}

static void WINAPI DSP_Free(void *inst)
{
}

static void WINAPI DSP_Config(void *inst, HWND win)
{
}

static DWORD WINAPI DSP_GetConfig(void *inst, void *config)
{
    memcpy(config, &pluginConf, sizeof(pluginConf));
    return sizeof(pluginConf); // return size of config info
}

static BOOL WINAPI DSP_SetConfig(void *inst, void *config, DWORD size)
{
    memcpy(&pluginConf, config, min(size, sizeof(pluginConf)));
    return TRUE;
}

// get the plugin's XMPDSP interface
XMPDSP *WINAPI XMPDSP_GetInterface2(DWORD face, InterfaceProc faceproc)
{
    if (face != XMPDSP_FACE) return NULL;
    xmpfmisc = (XMPFUNC_MISC*)faceproc(XMPFUNC_MISC_FACE); // import "misc" functions
    return &dsp;
}

BOOL WINAPI DllMain(HINSTANCE hDLL, DWORD reason, LPVOID reserved)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        ghInstance = hDLL;
        DisableThreadLibraryCalls(hDLL);
    }
    return 1;
}
