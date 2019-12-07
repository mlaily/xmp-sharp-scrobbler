using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace XmpSharpScrobbler.PluginInfrastructure
{
#pragma warning disable CA1051 // Do not declare visible instance fields

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

    public delegate ScrobblerConfig AskUserForNewAuthorizedSessionKey(IntPtr ownerWindowHandle);
    public delegate void SetSessionKey(ScrobblerConfig scrobblerConfig);

    public delegate void OnTrackStartsPlaying(
        [MarshalAs(UnmanagedType.LPWStr)]string title,
        [MarshalAs(UnmanagedType.LPWStr)]string artist,
        [MarshalAs(UnmanagedType.LPWStr)]string album,
        [MarshalAs(UnmanagedType.LPWStr)]string trackNumber,
        int durationMs);

    public delegate void OnTrackCanScrobble(
        [MarshalAs(UnmanagedType.LPWStr)]string title,
        [MarshalAs(UnmanagedType.LPWStr)]string artist,
        [MarshalAs(UnmanagedType.LPWStr)]string album,
        [MarshalAs(UnmanagedType.LPWStr)]string trackNumber,
        int durationMs,
        long utcUnixTimestamp);

#pragma warning restore CA1051 // Do not declare visible instance fields
}
