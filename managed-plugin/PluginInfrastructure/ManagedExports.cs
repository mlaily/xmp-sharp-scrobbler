using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace xmp_sharp_scrobbler.PluginInfrastructure
{
    /// <summary>
    /// Managed methods available to the native plugin.
    /// See managed-plugin-initializer.h for the native mirror.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class ManagedExports
    {
        public Action FreeManagedExports;
        public Log LogInfo;
        public Log LogWarning;
        public Log LogVerbose;
        public AskUserForNewAuthorizedSessionKey AskUserForNewAuthorizedSessionKey;
        public SetSessionKey SetSessionKey;
        public OnTrackStartsPlaying OnTrackStartsPlaying;
        public OnTrackCanScrobble OnTrackCanScrobble;
        public Action OnTrackCompletes;
    }

    public delegate void Log([MarshalAs(UnmanagedType.LPWStr)]string text);

    [return: MarshalAs(UnmanagedType.LPStr)] public delegate string AskUserForNewAuthorizedSessionKey(IntPtr ownerWindowHandle);
    public delegate void SetSessionKey([MarshalAs(UnmanagedType.LPStr)]string key);

    public delegate void OnTrackCanScrobble(
        [MarshalAs(UnmanagedType.LPWStr)]string artist,
        [MarshalAs(UnmanagedType.LPWStr)]string track,
        [MarshalAs(UnmanagedType.LPWStr)]string album,
        int durationMs,
        [MarshalAs(UnmanagedType.LPWStr)]string trackNumber,
        [MarshalAs(UnmanagedType.LPWStr)]string mbid,
        long utcUnixTimestamp);

    public delegate void OnTrackStartsPlaying(
        [MarshalAs(UnmanagedType.LPWStr)]string artist,
        [MarshalAs(UnmanagedType.LPWStr)]string track,
        [MarshalAs(UnmanagedType.LPWStr)]string album,
        int durationMs,
        [MarshalAs(UnmanagedType.LPWStr)]string trackNumber,
        [MarshalAs(UnmanagedType.LPWStr)]string mbid);
}
