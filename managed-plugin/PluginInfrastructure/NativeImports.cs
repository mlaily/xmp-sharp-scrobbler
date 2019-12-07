using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace XmpSharpScrobbler.PluginInfrastructure
{
    /// <summary>
    /// Native methods available to the managed plugin are declared as DllImports here.
    /// See main.h for the native exports.
    /// </summary>
    internal static class NativeImports
    {
        private const string DllName = "xmp-sharp-scrobbler";

        /// <summary>
        /// See managed-plugin-initializer.h for the native export of this method.
        /// </summary>
        [DllImport(DllName)]
        internal static extern void InitializeManagedExports([In, Out]IntPtr exports);

        //

        [DllImport(DllName)]
        internal static extern void ShowInfoBubble([In, MarshalAs(UnmanagedType.LPWStr)]string text, int displayTimeMs);
    }
}
