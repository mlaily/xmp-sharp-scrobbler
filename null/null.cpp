// XMPlay NULL output plugin (c) 2009-2013 Ian Luck

#include <windows.h>
#include <shlobj.h>
#include <stdio.h>
#include "xmpout.h"

static XMPFUNC_OUT *xmpfout;
static XMPFUNC_MISC *xmpfmisc;
static XMPFUNC_REGISTRY *xmpfreg;
static XMPFUNC_TEXT *xmpftext;

static BOOL isWinNT;
static BOOL logging=TRUE;

class Lock {
	CRITICAL_SECTION *_lock;
public:
	Lock(CRITICAL_SECTION &lock) : _lock(&lock) {EnterCriticalSection(_lock);}
	~Lock() {LeaveCriticalSection(_lock);}
};

static CRITICAL_SECTION loglock;

typedef struct LOGENTRY {
	LOGENTRY *next;
	char *file;
	DWORD starttime;
	DWORD endtime;
	DWORD bytes;
} LOGENTRY;

static LOGENTRY *logentry=NULL;

static void ClearLog()
{
	Lock lock(loglock);
	if (logentry) {
		LOGENTRY *e=logentry;
		while (e) {
			LOGENTRY *n=e->next;
			free(e->file);
			free(e);
			e=n;
		}
		logentry=0;
	}
}

static BOOL CALLBACK OUT_Config(HWND h, UINT m, WPARAM w, LPARAM l);
static const char *WINAPI OUT_GetName(DWORD output);
static DWORD WINAPI OUT_GetFlags(DWORD output);
static BOOL WINAPI OUT_Open(DWORD output, XMPOUT_FORMAT *form, HANDLE event);
static void WINAPI OUT_Close();
static BOOL WINAPI OUT_Reset();
static BOOL WINAPI OUT_Pause(BOOL resume);
static void WINAPI OUT_SetVolume(float volume, float balance);
static DWORD WINAPI OUT_CanWrite();
static BOOL WINAPI OUT_Write(const void *buf, DWORD length);
static DWORD WINAPI OUT_GetBuffered();
static void WINAPI OUT_GetGeneralInfo(char *buf);
static void WINAPI OUT_NewTrack(const char *file);

// plugin interface
static const XMPOUT xmpout={
	0,
	"NULL",
	OUT_Config,
	OUT_GetName,
	OUT_GetFlags,
	OUT_Open,
	OUT_Close,
	OUT_Reset,
	OUT_Pause,
	OUT_SetVolume,
	OUT_CanWrite,
	OUT_Write,
	OUT_GetBuffered,
	OUT_GetGeneralInfo,
	OUT_NewTrack
};

// get output name
static const char *WINAPI OUT_GetName(DWORD output)
{
	if (output>0) return NULL; // only got 1 output
	return "NULL";
}

// get output flags (XMPOUT_OUTPUT_xxx)
static DWORD WINAPI OUT_GetFlags(DWORD output)
{
	return XMPOUT_OUTPUT_FILE|XMPOUT_OUTPUT_VOLUME|XMPOUT_OUTPUT_NOAMP; // enable file writing mode, and disable XMPlay's volume and amplification processing
}

// start output
static BOOL WINAPI OUT_Open(DWORD output, XMPOUT_FORMAT *form, HANDLE event)
{
	return TRUE;
}

// close output
static void WINAPI OUT_Close()
{
}

// reset
static BOOL WINAPI OUT_Reset()
{
	return TRUE;
}

// pause/resume
static BOOL WINAPI OUT_Pause(BOOL resume)
{
	return TRUE;
}

// set volume & balance
static void WINAPI OUT_SetVolume(float volume, float balance)
{
}

// get amount of data (in bytes) that can be written
static DWORD WINAPI OUT_CanWrite()
{
	return 0x100000;
}

// write data (length in bytes)
static BOOL WINAPI OUT_Write(const void *buf, DWORD length)
{
	Lock lock(loglock);
	if (logentry) {
		logentry->bytes+=length;
		logentry->endtime=GetTickCount();
	}
	return TRUE;
}

// get amount of buffered data (bytes) yet to play
static DWORD WINAPI OUT_GetBuffered()
{
	return 0; // no buffering
}

// get General info window text
void WINAPI OUT_GetGeneralInfo(char *buf)
{
//	xmpfmisc->AddInfoText(buf,"heading","blah blah blah...");
}

// new track
static void WINAPI OUT_NewTrack(const char *file)
{
	if (logging) {
		Lock lock(loglock);
		LOGENTRY *e=(LOGENTRY*)calloc(1,sizeof(LOGENTRY));
		e->file=strdup(file);
		e->starttime=GetTickCount();
		if (logentry) e->next=logentry;
		logentry=e;
	}
}

#define ITEM(id) GetDlgItem(h,id)
#define MESS(id,m,w,l) SendDlgItemMessage(h,id,m,(WPARAM)(w),(LPARAM)(l))

// options page handler (dialog in resource 1000)
static BOOL CALLBACK OUT_Config(HWND h, UINT m, WPARAM w, LPARAM l)
{
	switch (m) {
		case WM_COMMAND:
			switch (LOWORD(w)) {
				case 10: // logging switch
					EnableWindow(ITEM(1000),TRUE); // enable "Apply" button
					break;
				case 12: // clear the log
					ClearLog();
					MESS(11,LVM_DELETEALLITEMS,0,0);
					break;
				case 1000: // Apply
					{
						logging=MESS(10,BM_GETCHECK,0,0);
						EnableWindow(ITEM(1000),FALSE);
					}
					break;
			}
			return 1;
		
		case WM_SIZE:
			{ // move version number to bottom-right corner
				HWND v=ITEM(65534);
				RECT r;
				GetClientRect(v,&r);
				SetWindowPos(v,0,LOWORD(l)-r.right-2,HIWORD(l)-r.bottom,0,0,SWP_NOSIZE|SWP_NOZORDER);
			}
			return 1;

		case WM_INITDIALOG:
			MESS(10,BM_SETCHECK,logging,0);
			{
				LVCOLUMN c={LVCF_TEXT|LVCF_WIDTH,LVCFMT_LEFT,224,"File"};
				MESS(11,LVM_INSERTCOLUMN,0,&c);
				c.pszText="Processed";
				c.iSubItem=1;
				c.cx=100;
				MESS(11,LVM_INSERTCOLUMN,1,&c);
				MESS(11,LVM_SETEXTENDEDLISTVIEWSTYLE,LVS_EX_FULLROWSELECT|LVS_EX_INFOTIP,LVS_EX_FULLROWSELECT|LVS_EX_INFOTIP);
			}
			{ // add the log entries to the log display
				Lock lock(loglock);
				LOGENTRY *e=logentry;
				while (e) {
					if (e->endtime) {
						LVITEM i={LVIF_TEXT,0x7fffffff};
						if (isWinNT) { // translate UTF-8 filename to UTF-16 on NT
							int n=Utf2Uni(e->file,-1,0,0);
							WCHAR *uni=(WCHAR*)malloc(n*2);
							Utf2Uni(e->file,-1,uni,n);
							i.pszText=(char*)uni;
							i.iItem=MESS(11,LVM_INSERTITEMW,0,&i);
							free(uni);
						} else {
							i.pszText=e->file;
							i.iItem=MESS(11,LVM_INSERTITEM,0,&i);
						}
						char buf[32];
						sprintf(buf,"%.1fMB in %.1fs",e->bytes/1000000.0,(e->endtime-e->starttime)/1000.0);
						i.iSubItem=1;
						i.pszText=buf;
						MESS(11,LVM_SETITEM,0,&i);
					}
					e=e->next;
				}
			}
			return 1;
	}
	return 0;
}

// get the plugin's XMPOUT interface
const XMPOUT *WINAPI XMPOUT_GetInterface(DWORD face, InterfaceProc faceproc)
{
	if (face!=XMPOUT_FACE) return NULL;
	xmpfout=(XMPFUNC_OUT*)faceproc(XMPFUNC_OUT_FACE);
	xmpfmisc=(XMPFUNC_MISC*)faceproc(XMPFUNC_MISC_FACE);
	xmpfreg=(XMPFUNC_REGISTRY*)faceproc(XMPFUNC_REGISTRY_FACE);
	xmpftext=(XMPFUNC_TEXT*)faceproc(XMPFUNC_TEXT_FACE);
	xmpfreg->GetInt("NULL output","Logging",&logging); // get config
	return &xmpout;
}

BOOL WINAPI DllMain(HINSTANCE hDLL, DWORD reason, LPVOID reserved)
{
	switch (reason) {
		case DLL_PROCESS_ATTACH:
			DisableThreadLibraryCalls(hDLL);
			isWinNT=(int)GetVersion()>=0;
			InitializeCriticalSection(&loglock);
			break;

		case DLL_PROCESS_DETACH:
			ClearLog();
			DeleteCriticalSection(&loglock);
			if (xmpfreg) { // store config
				xmpfreg->SetInt("NULL output","Logging",&logging);
			}
			break;
	}
	return TRUE;
}
