// XMPlay Flanger DSP plugin (c) 2004-2013 Ian Luck

#include <windows.h>
#include <commctrl.h>
#include <math.h>
#include "xmpdsp.h"

static HINSTANCE dllinst;
static HWND confwin=0;

static XMPFUNC_MISC *xmpfmisc=0;
static XMPFUNC_REGISTRY *xmpfreg=0;

// config structure
typedef struct {
	BOOL on;
	DWORD speed;
} FlangeConfig;

typedef struct {
	FlangeConfig conf;
	int chans;
	float *buf;
	int buflen,bufpos;
	float pos,inc;
} FlangeStuff;

static FlangeStuff *flange=NULL;

static int defspeed=50;

static void *WINAPI DSP_New();
static void WINAPI DSP_Free(void *inst);
static const char *WINAPI DSP_GetDescription(void *inst);
static void WINAPI DSP_Config(void *inst, HWND win);
static DWORD WINAPI DSP_GetConfig(void *inst, void *config);
static BOOL WINAPI DSP_SetConfig(void *inst, void *config, DWORD size);
static void WINAPI DSP_NewTrack(void *inst, const char *file);
static void WINAPI DSP_SetFormat(void *inst, const XMPFORMAT *form);
static void WINAPI DSP_Reset(void *inst);
static DWORD WINAPI DSP_Process(void *inst, float *srce, DWORD count);

static void WINAPI DSP_About(HWND win)
{
	MessageBox(win,"blah blah blah...","Flanger DSP example",MB_ICONINFORMATION);
}

// plugin interface
static XMPDSP xmpdsp={
	0, // doesn't support multiple instances or have a tail
	"Flanger",
	DSP_About,
	DSP_New,
	DSP_Free,
	DSP_GetDescription,
	DSP_Config,
	DSP_GetConfig,
	DSP_SetConfig,
	DSP_NewTrack,
	DSP_SetFormat,
	DSP_Reset,
	DSP_Process
};

static void SetFlangeSpeed(FlangeStuff *flange)
{
	flange->inc=0.00003f*pow(2,flange->conf.speed/12.0f);
}

// new DSP instance
static void *WINAPI DSP_New()
{
	flange=(FlangeStuff*)calloc(1,sizeof(*flange));
	flange->conf.on=TRUE;
	flange->conf.speed=defspeed;
	return flange;
}

// free DSP instance
static void WINAPI DSP_Free(void *inst)
{
	if (confwin) EndDialog(confwin,0); // close config window before freeing the DSP
	free(flange);
	flange=NULL;
}

// get description for plugin list
static const char *WINAPI DSP_GetDescription(void *inst)
{
	return xmpdsp.name;
}

#define MESS(id,m,w,l) SendDlgItemMessage(h,id,m,(WPARAM)(w),(LPARAM)(l))

static BOOL CALLBACK DSPDialogProc(HWND h, UINT m, WPARAM w, LPARAM l)
{
	switch (m) {
		case WM_COMMAND:
			switch (LOWORD(w)) {
				case IDCANCEL:
					EndDialog(h,0);
					break;
				case 10:
					DSP_Reset(flange);
					flange->conf.on=MESS(10,BM_GETCHECK,0,0);
					break;
			}
			return 1;

		case WM_VSCROLL:
			if (l) {
				defspeed=flange->conf.speed=100-SendMessage((HWND)l,TBM_GETPOS,0,0);;
				SetFlangeSpeed(flange);
			}
			return 1;

		case WM_INITDIALOG:
			confwin=h;
			MESS(10,BM_SETCHECK,flange->conf.on,0);
			MESS(11,TBM_SETRANGE,FALSE,MAKELONG(0,100));
			MESS(11,TBM_SETPOS,TRUE,100-flange->conf.speed);
			return 1;

		case WM_DESTROY:
			confwin=0;
			break;
	}
	return 0;
}

// show config options
static void WINAPI DSP_Config(void *inst, HWND win)
{
	DialogBox(dllinst,(char*)1000,win,&DSPDialogProc);
}

// get DSP config
static DWORD WINAPI DSP_GetConfig(void *inst, void *config)
{
	FlangeStuff *flange=(FlangeStuff*)inst;
	memcpy(config,&flange->conf,sizeof(flange->conf));
	return sizeof(*flange); // return size of config info
}

// set DSP config
static BOOL WINAPI DSP_SetConfig(void *inst, void *config, DWORD size)
{
	FlangeStuff *flange=(FlangeStuff*)inst;
	memcpy(&flange->conf,config,sizeof(flange->conf));
	return TRUE;
}

// reset DSP when seeking
static void WINAPI DSP_Reset(void *inst)
{
	FlangeStuff *flange=(FlangeStuff*)inst;
	flange->bufpos=0;
	flange->pos=flange->buflen/2;
	if (flange->buf) memset(flange->buf,0,flange->buflen*flange->chans*sizeof(float));
}

// new track has been opened (or closed if file=NULL)
static void WINAPI DSP_NewTrack(void *inst, const char *file)
{
}

// set the sample format at start (or end if form=NULL) of playback
static void WINAPI DSP_SetFormat(void *inst, const XMPFORMAT *form)
{
	FlangeStuff *flange=(FlangeStuff*)inst;
	if (form) {
		flange->chans=form->chan;
		flange->buflen=form->rate/125;
		flange->buf=(float*)malloc(flange->buflen*flange->chans*sizeof(float));
		SetFlangeSpeed(flange);
		DSP_Reset(flange);
	} else {
		free(flange->buf);
		flange->buf=0;
	}
}

// process the sample data
static DWORD WINAPI DSP_Process(void *inst, float *buffer, DWORD count)
{
	int a,b;
	FlangeStuff *flange=(FlangeStuff*)inst;
	if (flange->conf.on) {
		for (a=count;a;a--) {
			int p1=(flange->bufpos+(int)flange->pos)%flange->buflen;
			int p2=(p1+1)%flange->buflen;
			float *fb1=flange->buf+p1*flange->chans;
			float *fb2=flange->buf+p2*flange->chans;
			float *fb3=flange->buf+flange->bufpos*flange->chans;
			float f=flange->pos-(int)flange->pos;
			for (b=0;b<flange->chans;b++) {
				float s=(*buffer+((fb1[b]*(1-f))+(fb2[b]*f)))*0.7;
				fb3[b]=*buffer;
				*buffer++=s;
			}
			if (++flange->bufpos==flange->buflen) flange->bufpos=0;
			flange->pos+=flange->inc;
			if (flange->pos<0 || flange->pos>=flange->buflen-1) {
				flange->inc=-flange->inc;
				flange->pos+=flange->inc;
			}
		}
	}
	return count;
}

// shortcut to enable the DSP
static void WINAPI ShortcutOn()
{
	if (flange) { // make sure the DSP is setup first
		DSP_Reset(flange);
		flange->conf.on^=1;
		if (confwin) SendDlgItemMessage(confwin,10,BM_SETCHECK,flange->conf.on,0); // update config window if it's open
		xmpfmisc->ShowBubble(flange->conf.on?"Flanger = on":"Flanger = off",0); // show a bubble to the user
	}
}

static const XMPSHORTCUT shortcut={0x10000,"DSP - Flanger on/off",ShortcutOn};

// get the plugin's XMPDSP interface
XMPDSP *WINAPI XMPDSP_GetInterface2(DWORD face, InterfaceProc faceproc)
{
	if (face!=XMPDSP_FACE) return NULL;
	xmpfmisc=(XMPFUNC_MISC*)faceproc(XMPFUNC_MISC_FACE); // import "misc" functions
	xmpfreg=(XMPFUNC_REGISTRY*)faceproc(XMPFUNC_REGISTRY_FACE); // import "misc" functions
	xmpfreg->GetInt("Flange","Speed",&defspeed); // get the default speed setting
	xmpfmisc->RegisterShortcut(&shortcut); // register a shortcut
	return &xmpdsp;
}

BOOL WINAPI DllMain(HINSTANCE hDLL, DWORD reason, LPVOID reserved)
{
	switch (reason) {
		case DLL_PROCESS_ATTACH:
			DisableThreadLibraryCalls(hDLL);
			dllinst=hDLL;
			break;
		case DLL_PROCESS_DETACH:
			if (xmpfreg) {
				xmpfreg->SetInt("Flange","Speed",&defspeed); // store the default speed setting
			}
			break;
	}
	return TRUE;
}
