using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace XmpSharpScrobbler.PluginInfrastructure
{
#pragma warning disable CA1051 // Do not declare visible instance fields

    [StructLayout(LayoutKind.Sequential)]
    public class ScrobblerConfig
    {
        /// <summary>
        /// bytes. This is an ansi string.
        /// </summary>
        private const int SessionKeySize = 32;

        /// <summary>
        /// bytes. This is a unicode string.
        /// </summary>
        private const int UserNameSize = 128 * 2;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SessionKeySize)]
        public readonly byte[] sessionKey;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = UserNameSize)]
        public readonly byte[] userName;

        public ScrobblerConfig()
        {
            sessionKey = new byte[SessionKeySize];
            userName = new byte[UserNameSize];
        }

        public string SessionKey
        {
            get => Encoding.ASCII.GetString(sessionKey).TrimEnd('\0');
            set
            {
                if (value == null)
                {
                    Array.Clear(sessionKey, 0, SessionKeySize);
                }
                else
                {
                    var bytes = Encoding.ASCII.GetBytes(value);
                    Array.Copy(bytes, 0, sessionKey, 0, Math.Min(SessionKeySize, bytes.Length));
                }
            }
        }

        public string UserName
        {
            get => Encoding.Unicode.GetString(userName).TrimEnd('\0');
            set
            {
                if (value == null)
                {
                    Array.Clear(userName, 0, UserNameSize);
                }
                else
                {
                    var bytes = Encoding.Unicode.GetBytes(value);
                    Array.Copy(bytes, 0, userName, 0, Math.Min(UserNameSize, bytes.Length));
                }
            }
        }
    }
}
#pragma warning restore CA1051 // Do not declare visible instance fields