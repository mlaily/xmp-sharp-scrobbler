// XMPlay FLAC input plugin (c) 2005-2013 Ian Luck
// requires libflac and libogg libraries

#include "xmpin.h"

static XMPFUNC_IN *xmpfin;
static XMPFUNC_MISC *xmpfmisc;
static XMPFUNC_FILE *xmpffile;
static XMPFUNC_TEXT *xmpftext;
static DWORD xmpver;

#define FLAC__NO_DLL
#include "FLAC/stream_decoder.h"

typedef struct
{
	XMPFILE file;
	FLAC__StreamDecoder *flac;
	FLAC__StreamMetadata_StreamInfo info;
	BOOL ogg;
	DWORD buflen,bufp,brate,tagc;
	int *buf;
	char **tags;
	BOOL eof;
	QWORD pos;
} FLAC;

static FLAC *flac=NULL;

static FLAC__StreamDecoderWriteStatus write_callback_(const FLAC__StreamDecoder *decoder, const FLAC__Frame *frame, const FLAC__int32 * const buffer[], void *client_data)
{
	FLAC *flac=(FLAC*)client_data;
	int *d=flac->buf;
	DWORD a=0;
	do {
		DWORD b=0;
		do {
			*d++=buffer[b][a];
		} while (++b<flac->info.channels);
	} while (++a<frame->header.blocksize);
	flac->buflen=frame->header.blocksize*flac->info.channels;
	flac->bufp=0;
	flac->pos=frame->header.number.sample_number+frame->header.blocksize;
	return FLAC__STREAM_DECODER_WRITE_STATUS_CONTINUE;
}

static void metadata_callback_(const FLAC__StreamDecoder *decoder, const FLAC__StreamMetadata *metadata, void *client_data)
{
	FLAC *flac=(FLAC*)client_data;
	if (metadata->type==FLAC__METADATA_TYPE_STREAMINFO) {
		flac->info=metadata->data.stream_info;
	} else if (metadata->type==FLAC__METADATA_TYPE_VORBIS_COMMENT && !flac->tags) {
		const FLAC__StreamMetadata_VorbisComment *vc=&metadata->data.vorbis_comment;
		char **tags=(char**)malloc((vc->num_comments+1)*sizeof(char*));
		DWORD a;
		tags[0]=xmpftext->Utf8((char*)vc->vendor_string.entry,vc->vendor_string.length); // FLAC has UTF-8 tags
		for (a=0;a<vc->num_comments;a++)
			tags[1+a]=xmpftext->Utf8((char*)vc->comments[a].entry,vc->comments[a].length);
		flac->tags=tags;
		flac->tagc=vc->num_comments;
	}
}

static void error_callback_(const FLAC__StreamDecoder *decoder, FLAC__StreamDecoderErrorStatus status, void *client_data)
{
}

// FLAC file callbacks - simply passing them on to the XMPFILE routines
static FLAC__StreamDecoderReadStatus read_callback_(const FLAC__StreamDecoder *decoder, FLAC__byte buffer[], size_t *bytes, void *client_data)
{
	FLAC *flac=(FLAC*)client_data;
	DWORD count=*bytes;
	if (!count) return FLAC__STREAM_DECODER_READ_STATUS_ABORT;
	while (1) {
		*bytes=xmpffile->Read(flac->file,buffer,count);
		if (!*bytes && xmpffile->NetIsActive(flac->file)) { // out of data but still connected...
			if (!flac->buflen) return FLAC__STREAM_DECODER_READ_STATUS_CONTINUE; // avoid buffering delay while seeking
			if (xmpffile->NetPreBuf(flac->file)) // pre-buffer some more
				continue;
		}
		if (!*bytes) {
			flac->eof=TRUE;
			return FLAC__STREAM_DECODER_READ_STATUS_END_OF_STREAM;
		}
		return FLAC__STREAM_DECODER_READ_STATUS_CONTINUE;
	}
}
static FLAC__StreamDecoderSeekStatus seek_callback_(const FLAC__StreamDecoder *decoder, FLAC__uint64 absolute_byte_offset, void *client_data)
{
	FLAC *flac=(FLAC*)client_data;
	if (!xmpffile->Seek(flac->file,(DWORD)absolute_byte_offset))
		return FLAC__STREAM_DECODER_SEEK_STATUS_ERROR;
	flac->eof=FALSE;
	return FLAC__STREAM_DECODER_SEEK_STATUS_OK;
}
static FLAC__StreamDecoderTellStatus tell_callback_(const FLAC__StreamDecoder *decoder, FLAC__uint64 *absolute_byte_offset, void *client_data)
{
	FLAC *flac=(FLAC*)client_data;
	*absolute_byte_offset=xmpffile->Tell(flac->file);
	return FLAC__STREAM_DECODER_TELL_STATUS_OK;
}
static FLAC__StreamDecoderLengthStatus length_callback_(const FLAC__StreamDecoder *decoder, FLAC__uint64 *stream_length, void *client_data)
{
	FLAC *flac=(FLAC*)client_data;
	*stream_length=xmpffile->GetSize(flac->file);
	return FLAC__STREAM_DECODER_LENGTH_STATUS_OK;
}
static FLAC__bool eof_callback_(const FLAC__StreamDecoder *decoder, void *client_data)
{
	FLAC *flac=(FLAC*)client_data;
	return flac->eof;
}

static void FreeFLAC(FLAC *flac)
{
	FLAC__stream_decoder_delete(flac->flac);
	if (flac->tags) {
		DWORD a;
		for (a=0;a<=flac->tagc;a++) xmpfmisc->Free(flac->tags[a]);
		free(flac->tags);
	}
	free(flac->buf);
	free(flac);
}

static FLAC *InitFLAC(XMPFILE file)
{
	FLAC *flac;
	BOOL ogg=FALSE;
	DWORD sig;
	xmpffile->Read(file,&sig,4);
	if (sig==MAKEFOURCC('O','g','g','S'))
		ogg=TRUE;
	else if (sig!=MAKEFOURCC('f','L','a','C'))
		return NULL;
	xmpffile->Seek(file,0);

	flac=(FLAC*)calloc(1,sizeof(FLAC));
	flac->file=file;
	flac->ogg=ogg;
	flac->flac=FLAC__stream_decoder_new();
	FLAC__stream_decoder_set_metadata_respond(flac->flac,FLAC__METADATA_TYPE_VORBIS_COMMENT);
	if ((ogg?FLAC__stream_decoder_init_ogg_stream:FLAC__stream_decoder_init_stream)(flac->flac, read_callback_, seek_callback_, tell_callback_, length_callback_, eof_callback_, write_callback_, metadata_callback_, error_callback_, flac)
		|| !FLAC__stream_decoder_process_until_end_of_metadata(flac->flac) || !flac->info.sample_rate) {
		FreeFLAC(flac);
		return NULL;
	}

	return flac;
}

// check if a file is playable by this plugin
// more thorough checks can be saved for the GetFileInfo and Open functions
static BOOL WINAPI FLAC_CheckFile(const char *filename, XMPFILE file)
{
	BYTE buf[33];
	xmpffile->Read(file,buf,sizeof(buf));
	return *(DWORD*)buf==MAKEFOURCC('f','L','a','C')
		|| (*(DWORD*)buf==MAKEFOURCC('O','g','g','S') && *(DWORD*)(buf+0x1d)==MAKEFOURCC('F','L','A','C'));
}

// get the tags as a series of NULL-terminated names and values
static char *GetTags(FLAC *flac)
{
	DWORD tagl=14;
	char *tags=(char*)xmpfmisc->Alloc(tagl+1);
	memcpy(tags,"filetype\0FLAC",14); // the filetype tag
	for (int a=0;a<flac->tagc;a++) {
		const char *tag=flac->tags[1+a];
		int n=strlen(tag)+1;
		const char *s=strchr(tag,'=');
		if (s && s[1]) { // got a tag name and value
			tags=(char*)xmpfmisc->ReAlloc(tags,tagl+n+1); // allocate more space for the tag
			memcpy(tags+tagl,tag,n);
			tags[tagl+s-tag]=0; // separate the name and value with a NULL
			tagl+=n;
		}
	}
	tags[tagl]=0; // terminating NULL
	return tags;
}

// get file info
// return: the number of subsongs
static DWORD WINAPI FLAC_GetFileInfo(const char *filename, XMPFILE file, float **length, char **tags)
{
	FLAC *flac=InitFLAC(file);
	if (!flac) return 0; // failed
	if (length && flac->info.total_samples) {
		float *lens=(float*)xmpfmisc->Alloc(sizeof(float)); // allocate array for length(s)
		lens[0]=(float)flac->info.total_samples/flac->info.sample_rate;
		*length=lens;
	}
	if (tags) *tags=GetTags(flac);
	FreeFLAC(flac);
	return 1; // 1 song
}

// open a file for playback
// return:  0=failed, 1=success, 2=success and XMPlay can close the file
static DWORD WINAPI FLAC_Open(const char *filename, XMPFILE file)
{
	flac=InitFLAC(file);
	if (!flac) return 0; // failed
	flac->buf=(int*)malloc(flac->info.max_blocksize*flac->info.channels*sizeof(int));
	if (flac->info.total_samples) {
		float length=(float)flac->info.total_samples/flac->info.sample_rate;
		xmpfin->SetLength(length,TRUE); // set length
		flac->brate=(DWORD)(xmpffile->GetSize(file)/length);
	}
	if (xmpffile->GetType(file)>XMPFILE_TYPE_FILE) // streaming...
		xmpffile->NetSetRate(file,flac->brate?flac->brate:flac->info.sample_rate*flac->info.channels*flac->info.bits_per_sample/16); // ...need to tell bitrate
	if (flac->tags) { // check for replaygain
		DWORD a;
		for (a=0;a<flac->tagc;a++) {
			char *tag=flac->tags[1+a];
			if (!strnicmp(tag,"replaygain_track_gain=",22))
				xmpfin->SetGain(0,(float)atof(tag+22)); // set "track" gain
			else if (!strnicmp(tag,"replaygain_album_gain=",22))
				xmpfin->SetGain(1,(float)atof(tag+22)); // set "album" gain
			else if (xmpver>=0x03040281 && !strnicmp(tag,"replaygain_track_peak=",22))
				xmpfin->SetGain(2,(float)atof(tag+22)); // set peak level (requires least 3.4.2.129)
		}
	}
	return 1;
}

// close the file
static void WINAPI FLAC_Close()
{
	FreeFLAC(flac);
	flac=0;
}

// get the tags
// return NULL to delay the title update when there are no tags (use UpdateTitle to request update when ready)
static char *WINAPI FLAC_GetTags()
{
	return GetTags(flac);
}

// set the sample format (in=user chosen format, out=file format if different)
static void WINAPI FLAC_SetFormat(XMPFORMAT *form)
{
	form->res=flac->info.bits_per_sample/8;
	form->chan=flac->info.channels;
	form->rate=flac->info.sample_rate;
}

// get the main panel info text
static void WINAPI FLAC_GetInfoText(char *format, char *length)
{
	if (format) { // format details...
		format+=sprintf(format,"FLAC - ");
		if (flac->brate) format+=sprintf(format,"%dkb/s - ",flac->brate/125);
		sprintf(format,"%dhz",flac->info.sample_rate);
	}
// "length" will already contain the time, but more info can be added to the end, eg...
//	if (length) sprintf(strchr(length,0)," - %d frames",number_of_frames);
}

// get text for "General" info window
// separate headings and values with a tab (\t), end each line with a carriage-return (\r)
static void WINAPI FLAC_GetGeneralInfo(char *buf)
{
	buf+=sprintf(buf,"Format\t%s",flac->ogg?"Ogg FLAC":"FLAC");
	if (flac->tags) buf+=sprintf(buf," (%s)\r",flac->tags[0]);
	else *buf++='\r';
	if (flac->brate) buf+=sprintf(buf,"Bit rate\t%d kbps\r",flac->brate/125);
	buf+=sprintf(buf,"Sample rate\t%d hz\rChannels\t%d\rResolution\t%u bit\r",
		flac->info.sample_rate,flac->info.channels,flac->info.bits_per_sample);
}

// get text for "Message" info window
// separate tag names and values with a tab (\t), and end each line with a carriage-return (\r)
static void WINAPI FLAC_GetMessage(char *buf)
{
	if (flac->tags) {
		DWORD a;
		for (a=0;a<flac->tagc;a++) {
			char *tag=flac->tags[1+a],*p;
			if (!strnicmp(tag,"cuesheet=",9)) continue; // skip CUE sheet as it's already shown by XMPlay
			if (p=strchr(tag,'=')) {
				*p=0;
				buf=xmpfmisc->FormatInfoText(buf,tag,p+1);
				*p='=';
			} else
				buf=xmpfmisc->FormatInfoText(buf,0,tag);
		}
	}
}

// get some sample data, always floating-point
// count=number of floats to write (not bytes or samples)
// return number of floats written. if it's less than requested, playback is ended...
// so wait for more if there is more to come (use CheckCancel function to check if user wants to cancel)
static DWORD WINAPI FLAC_Process(float *buffer, DWORD count)
{
	DWORD done=0,c;
	while (done<count) {
		if (flac->bufp==flac->buflen) {
			if (FLAC__stream_decoder_get_state(flac->flac)==FLAC__STREAM_DECODER_END_OF_STREAM) {
				if (!flac->info.total_samples) { // length was unknown - get it now
					float length;
					flac->info.total_samples=flac->pos;
					length=(float)flac->info.total_samples/flac->info.sample_rate;
					xmpfin->SetLength(length,TRUE);
				}
				break;
			}
			if (!FLAC__stream_decoder_process_single(flac->flac)) {
				FLAC__stream_decoder_flush(flac->flac);
				continue;
			}
		}
		c=min(count-done,flac->buflen-flac->bufp);
		if (c) {
			int *src=flac->buf+flac->bufp;
			float scale=1.f/(1<<(flac->info.bits_per_sample-1));
			int a=c;
			do {
				*buffer++=*src++*scale;
			} while (--a);
		}
		flac->bufp+=c;
		done+=c;
	}
	return done;
}

// Get the seeking granularity in seconds
static double WINAPI FLAC_GetGranularity()
{
	return 0.001; // seek in millisecond units
}

// Seek to a position (in granularity units)
// return the new position in seconds (-1 = failed)
static double WINAPI FLAC_SetPosition(DWORD pos)
{
	double cpos=(double)flac->pos/flac->info.sample_rate; // current pos (in case seek fails)
	double time=pos*FLAC_GetGranularity(); // convert "pos" into seconds
	flac->bufp=flac->buflen=0;
	FLAC__stream_decoder_flush(flac->flac);
	if (!FLAC__stream_decoder_seek_absolute(flac->flac,(FLAC__uint64)(time*flac->info.sample_rate))) { // seek failed...
		if (pos) {
			int a;
			for (a=0;a<10 && (time-=3)>cpos;a++) { // step back a bit and try again up to 10 times
				FLAC__stream_decoder_flush(flac->flac);
				if (FLAC__stream_decoder_seek_absolute(flac->flac,(FLAC__uint64)(time*flac->info.sample_rate)))
					return time; // success!
			}
			// failed, so try going back to where it was
			FLAC__stream_decoder_flush(flac->flac);
			if (FLAC__stream_decoder_seek_absolute(flac->flac,(FLAC__uint64)(cpos*flac->info.sample_rate)))
				return cpos; // success!
			// failed again, just restart
			FLAC__stream_decoder_flush(flac->flac);
			if (FLAC__stream_decoder_seek_absolute(flac->flac,0))
				return 0;
			time=0;
		}
		xmpffile->Seek(flac->file,0);
	}
	return time;
}

// plugin interface
static XMPIN xmpin={
	XMPIN_FLAG_CANSTREAM, // can stream from 'net, and only using XMPFILE
	"FLAC (rev.9)",
	"FLAC\0flac/oga/ogg",
	NULL,
	NULL,
	FLAC_CheckFile,
	FLAC_GetFileInfo,
	FLAC_Open,
	FLAC_Close,
	NULL,
	FLAC_SetFormat,
	FLAC_GetTags,
	FLAC_GetInfoText,
	FLAC_GetGeneralInfo,
	FLAC_GetMessage,
	FLAC_SetPosition,
	FLAC_GetGranularity,
	NULL, // GetBuffering only applies when using your own file routines
	FLAC_Process,
	NULL, // WriteFile, see GetBuffering
	NULL, // don't have any "Samples"
	NULL, // nor any sub-songs
	NULL,
	NULL, // GetDownloaded, see GetBuffering
	// no built-in vis
};

// get the plugin's XMPIN interface
XMPIN *WINAPI XMPIN_GetInterface(DWORD face, InterfaceProc faceproc)
{
	if (face!=XMPIN_FACE) { // unsupported version
		static int shownerror=0;
		if (face<XMPIN_FACE && !shownerror) {
			MessageBox(0,"The XMP-FLAC plugin requires XMPlay 3.8 or above",0,MB_ICONEXCLAMATION);
			shownerror=1;
		}
		return NULL;
	}
	xmpfin=(XMPFUNC_IN*)faceproc(XMPFUNC_IN_FACE);
	xmpfmisc=(XMPFUNC_MISC*)faceproc(XMPFUNC_MISC_FACE);
	xmpffile=(XMPFUNC_FILE*)faceproc(XMPFUNC_FILE_FACE);
	xmpftext=(XMPFUNC_TEXT*)faceproc(XMPFUNC_TEXT_FACE);
	xmpver=xmpfmisc->GetVersion();
	return &xmpin;
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
