// XMPlay WAD archive plugin (c) 2006-2013 Ian Luck

#include "xmparc.h"

static XMPFUNC_FILE *xmpffile;
static XMPFUNC_MISC *xmpfmisc;

typedef struct {
	DWORD id;
	DWORD dirc;
	DWORD dirp;
} WADHEAD;

typedef struct {
	DWORD pos;
	DWORD size;
	char name[8];
} WADENTRY;

// check if a file can be handled by this plugin
// more thorough checks can be saved for the GetFileList and DecompressFile functions
BOOL WINAPI ARC_CheckFile(XMPFILE file)
{
	DWORD id;
	xmpffile->Read(file,&id,sizeof(id));
	return id==MAKEFOURCC('I','W','A','D') || id==MAKEFOURCC('P','W','A','D');
}

// get file list
// return a series of NULL-terminated entries
char *WINAPI ARC_GetFileList(XMPFILE file)
{
	char *fl=NULL;
	WADHEAD head;
	if (xmpffile->Read(file,&head,sizeof(head))==sizeof(head) && xmpffile->Seek(file,head.dirp)) {
		int p=0;
		for (int a=0;a<head.dirc;a++) {
			WADENTRY e;
			if (xmpffile->Read(file,&e,sizeof(e))!=sizeof(e)) break;
			fl=(char*)xmpfmisc->ReAlloc(fl,p+8+2); // allocate buffer (XMPlay will free it)
			memcpy(fl+p,e.name,8);
			fl[p+8]=0;
			p+=strlen(fl+p)+1;
			fl[p]=0;
		}
	}
	return fl;
}

// extract a file
// in: len=max amount wanted
// out: len=amount delivered
void *WINAPI ARC_DecompressFile(XMPFILE file, const char *entry, DWORD *len)
{
	if (strlen(entry)>8) return NULL;
	WADHEAD head;
	if (xmpffile->Read(file,&head,sizeof(head))==sizeof(head) && xmpffile->Seek(file,head.dirp)) {
		for (int a=0;a<head.dirc;a++) {
			WADENTRY e;
			if (xmpffile->Read(file,&e,sizeof(e))!=sizeof(e)) break;
			if (!strncmp(e.name,entry,8)) {
				if (!xmpffile->Seek(file,e.pos)) break;
				DWORD wanted=min(e.size,*len); // limit data to requested amount
				void *buf=xmpfmisc->Alloc(wanted); // allocate buffer (XMPlay will free it)
				if (!buf) break;
				*len=xmpffile->Read(file,buf,wanted);
				return buf; // return pointer to extracted file
			}
		}
	}
	return NULL;
}

// plugin interface
static XMPARC xmparc={
	0,
	"WAD archives\0wad",
	ARC_CheckFile,
	ARC_GetFileList,
	ARC_DecompressFile,
};

const XMPARC *WINAPI XMPARC_GetInterface(DWORD face, InterfaceProc faceproc)
{
	if (face!=XMPARC_FACE) return NULL;
	xmpffile=(XMPFUNC_FILE*)faceproc(XMPFUNC_FILE_FACE);
	xmpfmisc=(XMPFUNC_MISC*)faceproc(XMPFUNC_MISC_FACE);
	return &xmparc;
}

BOOL WINAPI DllMain(HINSTANCE hDLL, DWORD reason, LPVOID reserved)
{
	switch (reason) {
		case DLL_PROCESS_ATTACH:
			DisableThreadLibraryCalls(hDLL);
			break;
	}
	return TRUE;
}
