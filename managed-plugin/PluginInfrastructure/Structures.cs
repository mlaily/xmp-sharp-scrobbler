using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XmpSharpScrobbler.Misc;

namespace XmpSharpScrobbler.PluginInfrastructure
{
#pragma warning disable CA1051 // Do not declare visible instance fields

    [StructLayout(LayoutKind.Sequential)]
    public class ScrobblerConfig
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] sessionKey = new byte[32];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public readonly byte[] userName = new byte[128];

        public string SessionKey
        {
            get => InteropHelper.GetStringFromNativeBuffer(sessionKey, Encoding.ASCII);
            set => InteropHelper.SetNativeString(sessionKey, Encoding.ASCII, value);
        }

        public string UserName
        {
            get => InteropHelper.GetStringFromNativeBuffer(userName, Encoding.UTF8);
            set => InteropHelper.SetNativeString(userName, Encoding.UTF8, value);
        }
    }
}
#pragma warning restore CA1051 // Do not declare visible instance fields
