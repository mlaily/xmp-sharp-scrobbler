using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace XmpSharpScrobbler.PluginInfrastructure
{
#pragma warning disable CA1051 // Do not declare visible instance fields

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class ScrobblerConfig
    {
        /// <summary>
        /// chars, but ansi, so also bytes.
        /// </summary>
        private const int SessionKeySize = 32;

        /// <summary>
        /// bytes. Should have been a unicode string, but the marshaller does not allow mixing different fixed size strings of different charsets in a structure...
        /// </summary>
        private const int UserNameSize = 128 * 2;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SessionKeySize)]
        public string sessionKey;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = UserNameSize)]
        public readonly byte[] userName;

        public ScrobblerConfig()
        {
            userName = new byte[UserNameSize];
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
