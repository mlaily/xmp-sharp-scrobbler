// XMPlay plugin functions header
// new plugins can be submitted to plugins@xmplay.com

#pragma once

#include <wtypes.h>

typedef unsigned __int64 QWORD;

#ifdef __cplusplus
extern "C" {
#endif

// Note all texts are UTF-8 on WinNT based systems and ANSI on Win9x
#define Utf2Uni(src,slen,dst,dlen) MultiByteToWideChar(CP_UTF8,0,src,slen,dst,dlen) // convert UTF-8 to Windows Unicode/WideChar

typedef void *(WINAPI *InterfaceProc)(DWORD face); // XMPlay interface retrieval function received by plugin

#define XMPFUNC_MISC_FACE		0 // miscellaneous functions (XMPFUNC_MISC)
#define XMPFUNC_REGISTRY_FACE	1 // registry functions (XMPFUNC_REGISTRY)
#define XMPFUNC_FILE_FACE		2 // file functions (XMPFUNC_FILE)
#define XMPFUNC_TEXT_FACE		3 // text functions (XMPFUNC_TEXT)
#define XMPFUNC_STATUS_FACE		4 // playback status functions (XMPFUNC_STATUS)

#define XMPCONFIG_NET_BUFFER	0
#define XMPCONFIG_NET_RESTRICT	1
#define XMPCONFIG_NET_RECONNECT	2
#define XMPCONFIG_NET_PROXY		3
#define XMPCONFIG_NET_PROXYCONF	4 // pointer to string
#define XMPCONFIG_NET_TIMEOUT	5
#define XMPCONFIG_NET_PREBUF	6
#define XMPCONFIG_OUTPUT		7 // pointer to XMPFORMAT (version 3.6)

#define XMPINFO_TEXT_GENERAL	0 // General info text
#define XMPINFO_TEXT_MESSAGE	1 // Message info text
#define XMPINFO_TEXT_SAMPLES	2 // Samples info text

#define XMPINFO_REFRESH_MAIN	1 // main window info area
#define XMPINFO_REFRESH_GENERAL	2 // General info window
#define XMPINFO_REFRESH_MESSAGE	4 // Message info window
#define XMPINFO_REFRESH_SAMPLES	8 // Samples info window

typedef void *XMPFILE;

#define XMPFILE_TYPE_MEMORY		0 // file in memory
#define XMPFILE_TYPE_FILE		1 // local file
#define XMPFILE_TYPE_NETFILE	2 // file on the 'net
#define XMPFILE_TYPE_NETSTREAM	3 // 'net stream (unknown length)

typedef void (WINAPI *XMPSHORTCUTPROC)();
typedef void (WINAPI *XMPSHORTCUTPROCEX)(DWORD id);

typedef struct {
	DWORD id;				// must be unique and >=0x10000
	const char *text;		// description
	union {	// handler
		XMPSHORTCUTPROC proc;
		XMPSHORTCUTPROCEX procex; // if id&0x80000000
	};
} XMPSHORTCUT;

typedef struct {
	DWORD rate;		// sample rate
	DWORD chan;		// channels
	DWORD res;		// bytes per sample (1=8-bit,2=16-bit,3=24-bit,4=float,0=undefined)
} XMPFORMAT;

typedef struct {
	float time;		// cue position
	const char *title;
	const char *performer;
} XMPCUE;

#define TAG_FORMATTED_TITLE		(char*)-1 // formatted track title
#define TAG_FILENAME			(char*)-2 // filename
#define TAG_TRACK_TITLE			(char*)-3 // stream track (or CUE sheet) title
#define TAG_LENGTH				(char*)-4 // length in seconds
#define TAG_SUBSONGS			(char*)-5 // subsong count
#define TAG_SUBSONG				(char*)-6 // separated subsong (number/total)
#define TAG_RATING				(char*)-7 // user rating
#define TAG_TITLE				(char*)0 // = "title"
#define TAG_ARTIST				(char*)1 // = "artist"
#define TAG_ALBUM				(char*)2 // = "album"
#define TAG_DATE				(char*)3 // = "date"
#define TAG_TRACK				(char*)4 // = "title"
#define TAG_GENRE				(char*)5 // = "genre"
#define TAG_COMMENT				(char*)6 // = "comment"
#define TAG_FILETYPE			(char*)7 // = "filetype"

/*
	Non-"const" pointers returned by these functions should be freed via XMPFUNC_MISC:Free when done with them.
*/

typedef struct { // miscellaneous functions
	DWORD (WINAPI *GetVersion)(); // get XMPlay version (eg. 0x03040001 = 3.4.0.1)
	HWND (WINAPI *GetWindow)(); // get XMPlay window handle
	void *(WINAPI *Alloc)(DWORD len); // allocate memory
	void *(WINAPI *ReAlloc)(void *mem, DWORD len); // re-allocate memory
	void (WINAPI *Free)(void *mem); // free allocated memory/text
	BOOL (WINAPI *CheckCancel)(); // user wants to cancel?
	DWORD (WINAPI *GetConfig)(DWORD option); // get a config (XMPCONFIG_xxx) value
	const char *(WINAPI *GetSkinConfig)(const char *name); // get a skinconfig value
	void (WINAPI *ShowBubble)(const char *text, DWORD time); // show a help bubble (time in ms, 0=default)
	void (WINAPI *RefreshInfo)(DWORD mode); // refresh info displays (XMPINFO_REFRESH_xxx flags)
	char *(WINAPI *GetInfoText)(DWORD mode); // get info window text (XMPINFO_TEXT_xxx)
	char *(WINAPI *FormatInfoText)(char *buf, const char *name, const char *value); // format text for info window (tabs & new-lines)
	char *(WINAPI *GetTag)(const char *tag); // get a current track's tag (tag name or TAG_xxx)
	BOOL (WINAPI *RegisterShortcut)(const XMPSHORTCUT *cut); // add a shortcut
	BOOL (WINAPI *PerformShortcut)(DWORD id); // perform a shortcut action
// version 3.4.0.14
	const XMPCUE *(WINAPI *GetCue)(DWORD cue); // get a cue entry (0=image, 1=1st track)
// version 3.8
	BOOL (WINAPI *DDE)(const char *command); // execute a DDE command without using DDE
} XMPFUNC_MISC;

typedef struct { // "registry" functions
	DWORD (WINAPI *Get)(const char *section, const char *key, void *data, DWORD size); // if data=NULL, required size is returned
	DWORD (WINAPI *GetString)(const char *section, const char *key, char *data, DWORD size); // if data=NULL, required size is returned
	BOOL (WINAPI *GetInt)(const char *section, const char *key, int *data);
	BOOL (WINAPI *Set)(const char *section, const char *key, const void *data, DWORD size); // data=NULL = delete key
	BOOL (WINAPI *SetString)(const char *section, const char *key, const char *data);
	BOOL (WINAPI *SetInt)(const char *section, const char *key, const int *data);
} XMPFUNC_REGISTRY;

typedef struct { // file functions
	XMPFILE (WINAPI *Open)(const char *filename); // open a file
	XMPFILE (WINAPI *OpenMemory)(const void *buf, DWORD len); // open a file from memory
	void (WINAPI *Close)(XMPFILE file); // close an opened file
	DWORD (WINAPI *GetType)(XMPFILE file); // return XMPFILE_TYPE_xxx
	DWORD (WINAPI *GetSize)(XMPFILE file); // file size
	const char *(WINAPI *GetFilename)(XMPFILE file); // filename
	const void *(WINAPI *GetMemory)(XMPFILE file); // memory location (XMPFILE_TYPE_MEMORY)
	DWORD (WINAPI *Read)(XMPFILE file, void *buf, DWORD len); // read from file
	BOOL (WINAPI *Seek)(XMPFILE file, DWORD pos); // seek in file
	DWORD (WINAPI *Tell)(XMPFILE file); // get current file pos
	// net-only stuff
	void (WINAPI *NetSetRate)(XMPFILE file, DWORD rate); // set bitrate in bytes/sec (decides buffer size)
	BOOL (WINAPI *NetIsActive)(XMPFILE file); // connection is still up?
	BOOL (WINAPI *NetPreBuf)(XMPFILE file); // pre-buffer data
	DWORD (WINAPI *NetAvailable)(XMPFILE file); // get amount of data ready to go

	char *(WINAPI *ArchiveList)(XMPFILE file); // get archive contents (series of NULL-terminated entries)
	XMPFILE (WINAPI *ArchiveExtract)(XMPFILE file, const char *entry, DWORD len); // decompress file from archive
} XMPFUNC_FILE;

typedef struct { // text functions - return new string in native form (UTF-8/ANSI)
	char *(WINAPI *Ansi)(const char *text, int len); // ANSI string (len=-1=null terminated)
	char *(WINAPI *Unicode)(const WCHAR *text, int len); // Unicode string
	char *(WINAPI *Utf8)(const char *text, int len); // UTF-8 string
} XMPFUNC_TEXT;

typedef struct { // playback status functions
	BOOL (WINAPI *IsPlaying)(); // playing?
	double (WINAPI *GetTime)(); // track position in seconds
	QWORD (WINAPI *GetWritten)(); // samples written to output
	DWORD (WINAPI *GetLatency)(); // samples in output buffer
	const XMPFORMAT *(WINAPI *GetFormat)(BOOL in); // get input/output sample format
} XMPFUNC_STATUS;

// The following Winamp messages are also supported by XMPlay (see Winamp SDK for descriptions)
#define WM_WA_IPC WM_USER
#define IPC_DELETE 101
#define IPC_STARTPLAY 102   
#define IPC_ISPLAYING 104
#define IPC_GETOUTPUTTIME 105
#define IPC_JUMPTOTIME 106
#define IPC_SETPLAYLISTPOS 121
#define IPC_SETVOLUME 122
#define IPC_SETPANNING 123
#define IPC_GETLISTLENGTH 124
#define IPC_GETLISTPOS 125
#define IPC_GETPLAYLISTFILE 211
#define IPC_GETPLAYLISTTITLE 212

// The following is from the Winamp SDK headers (wa_ipc.h), along with some samples for XMPlay:

//HWND xmplayWinHandle = xmpfmisc->GetWindow();
//
///* SendMessage(hwnd_winamp,WM_WA_IPC,0,IPC_DELETE);
//** Use this api to clear Winamp's internal playlist.
//** You should not need to use IPC_DELETE_INT since it is used internally by Winamp when
//** it is dealing with some lame Windows Explorer issues (hard to believe that!).
//*/
//SendMessage(xmplayWinHandle, WM_WA_IPC, 0, IPC_DELETE);
//
///* SendMessage(hwnd_winamp,WM_WA_IPC,0,IPC_STARTPLAY);
//** Sending this will start playback and is almost the same as hitting the play button.
//** The IPC_STARTPLAY_INT version is used internally and you should not need to use it
//** since it won't be any fun.
//*/
//
///* SendMessage(hwnd_winamp,WM_WA_IPC,0,IPC_STARTPLAY);
//** Sending this will start playback and is almost the same as hitting the play button.
//** The IPC_STARTPLAY_INT version is used internally and you should not need to use it
//** since it won't be any fun.
//*/
//SendMessage(xmplayWinHandle, WM_WA_IPC, 0, IPC_STARTPLAY);
//
///* int res = SendMessage(hwnd_winamp,WM_WA_IPC,0,IPC_ISPLAYING);
//** This is sent to retrieve the current playback state of Winamp.
//** If it returns 1, Winamp is playing.
//** If it returns 3, Winamp is paused.
//** If it returns 0, Winamp is not playing.
//*/
//int isPlaying_WinampMsg = SendMessage(xmplayWinHandle, WM_WA_IPC, 0, IPC_ISPLAYING);
//
///* int res = SendMessage(hwnd_winamp,WM_WA_IPC,mode,IPC_GETOUTPUTTIME);
//** This api can return two different sets of information about current playback status.
//**
//** If mode = 0 then it will return the position (in ms) of the currently playing track.
//** Will return -1 if Winamp is not playing.
//**
//** If mode = 1 then it will return the current track length (in seconds).
//** Will return -1 if there are no tracks (or possibly if Winamp cannot get the length).
//*/
//int currentPositionMs = SendMessage(xmplayWinHandle, WM_WA_IPC, 0, IPC_GETOUTPUTTIME);
//int currentTrackLengthSeconds = SendMessage(xmplayWinHandle, WM_WA_IPC, 1, IPC_GETOUTPUTTIME);
//
///* (requires Winamp 1.60+)
//** SendMessage(hwnd_winamp,WM_WA_IPC,ms,IPC_JUMPTOTIME);
//** This api sets the current position (in milliseconds) for the currently playing song.
//** The resulting playback position may only be an approximate time since some playback
//** formats do not provide exact seeking e.g. mp3
//** This returns -1 if Winamp is not playing, 1 on end of file, or 0 if it was successful.
//*/
//SendMessage(xmplayWinHandle, WM_WA_IPC, 0, IPC_JUMPTOTIME);
//
///* (requires Winamp 2.0+)
//** SendMessage(hwnd_winamp,WM_WA_IPC,position,IPC_SETPLAYLISTPOS)
//** IPC_SETPLAYLISTPOS sets the playlist position to the specified 'position'.
//** It will not change playback status or anything else. It will just set the current
//** position in the playlist and will update the playlist view if necessary.
//**
//** If you use SendMessage(hwnd_winamp,WM_COMMAND,MAKEWPARAM(WINAMP_BUTTON2,0),0);
//** after using IPC_SETPLAYLISTPOS then Winamp will start playing the file at 'position'.
//*/
//SendMessage(xmplayWinHandle, WM_WA_IPC, 0, IPC_SETPLAYLISTPOS);
//
///* (requires Winamp 2.0+)
//** SendMessage(hwnd_winamp,WM_WA_IPC,volume,IPC_SETVOLUME);
//** IPC_SETVOLUME sets the volume of Winamp (between the range of 0 to 255).
//**
//** If you pass 'volume' as -666 then the message will return the current volume.
//** int curvol = SendMessage(hwnd_winamp,WM_WA_IPC,-666,IPC_SETVOLUME);
//*/
//SendMessage(xmplayWinHandle, WM_WA_IPC, 0, IPC_SETVOLUME);
//
///* (requires Winamp 2.0+)
//** SendMessage(hwnd_winamp,WM_WA_IPC,panning,IPC_SETPANNING);
//** IPC_SETPANNING sets the panning of Winamp from 0 (left) to 255 (right).
//**
//** At least in 5.x+ this works from -127 (left) to 127 (right).
//**
//** If you pass 'panning' as -666 to this api then it will return the current panning.
//** int curpan = SendMessage(hwnd_winamp,WM_WA_IPC,-666,IPC_SETPANNING);
//*/
//SendMessage(xmplayWinHandle, WM_WA_IPC, 0, IPC_SETPANNING);
//
///* (requires Winamp 2.0+)
//** int length = SendMessage(hwnd_winamp,WM_WA_IPC,0,IPC_GETLISTLENGTH);
//** IPC_GETLISTLENGTH returns the length of the current playlist as the number of tracks.
//*/
//int playlistTrackCount = SendMessage(xmplayWinHandle, WM_WA_IPC, 0, IPC_GETLISTLENGTH);
//
///* (requires Winamp 2.05+)
//** int pos=SendMessage(hwnd_winamp,WM_WA_IPC,0,IPC_GETLISTPOS);
//** IPC_GETLISTPOS returns the current playlist position (which is shown in the playlist
//** editor as a differently coloured text entry e.g is yellow for the classic skin).
//**
//** This api is a lot like IPC_WRITEPLAYLIST but a lot faster since it does not have to
//** write out the whole of the current playlist first.
//*/
// ZERO BASED ????
//int currentPlaylistPosition = SendMessage(xmplayWinHandle, WM_WA_IPC, 0, IPC_GETLISTPOS);
//
///* (requires Winamp 2.04+, only usable from plug-ins (not external apps))
//** char *name=SendMessage(hwnd_winamp,WM_WA_IPC,index,IPC_GETPLAYLISTFILE);
//** IPC_GETPLAYLISTFILE gets the filename of the playlist entry [index].
//** returns a pointer to it. returns NULL on error.
//*/
//int playlistIndex = 0;
//char* playlistIndexFileName = (char*)SendMessage(xmplayWinHandle, WM_WA_IPC, playlistIndex, IPC_GETPLAYLISTFILE);
//
///* (requires Winamp 2.04+, only usable from plug-ins (not external apps))
//** char *name=SendMessage(hwnd_winamp,WM_WA_IPC,index,IPC_GETPLAYLISTTITLE);
//**
//** IPC_GETPLAYLISTTITLE gets the title of the playlist entry [index].
//** returns a pointer to it. returns NULL on error.
//*/
//int playlistIndex = 0;
//char* playlistIndexTitle = (char*)SendMessage(xmplayWinHandle, WM_WA_IPC, playlistIndex, IPC_GETPLAYLISTTITLE);


#ifdef __cplusplus
}
#endif
