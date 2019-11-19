using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace xmp_sharp_scrobbler.PluginInfrastructure
{
    /// <summary>
    /// Native methods available to the managed plugin are declared as DllImports here.
    /// See main.h for the native exports.
    /// </summary>
    public static class NativeImports
    {
        private const string dllName = "xmp-sharp-scrobbler";

        /// <summary>
        /// See managed-plugin-initializer.h for the native export of this method.
        /// </summary>
        [DllImport(dllName)]
        public static extern void InitializeManagedExports([In, Out]IntPtr exports);

        //

        [DllImport(dllName)]
        public static extern void ShowInfoBubble([In, MarshalAs(UnmanagedType.LPWStr)]string text, int displayTimeMs);
    }
}
