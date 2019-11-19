// Copyright(c) 2015-2019 Melvyn Laïly
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using xmp_sharp_scrobbler;
using xmp_sharp_scrobbler.PluginInfrastructure;

// Warning: do not add a namespace and keep the "Plugin" name.
// This is the hardcoded type name when the native plugin loads the assembly and needs an entry point.

public static class Plugin
{
    private static ManagedExports ManagedExports;
    private static GCHandle ManagedExportsGCHandle;
    private static IntPtr pManagedExports;

    private static SharpScrobbler SharpScrobbler;
    private static GCHandle SharpScrobblerGCHandle;

    /// <summary>
    /// This is the plugin entry point.
    /// This method is called by the native plugin immediately after it loads the CLR.
    /// </summary>
    public static int EntryPoint(string arg)
    {
        SharpScrobbler = new SharpScrobbler();
        SharpScrobblerGCHandle = GCHandle.Alloc(SharpScrobbler);

        ManagedExports = new ManagedExports
        {
            FreeManagedExports = FreeManagedExports,
            LogInfo = LogInfo,
            LogWarning = LogWarning,
            LogVerbose = LogVerbose,
            AskUserForNewAuthorizedSessionKey = SharpScrobbler.AskUserForNewAuthorizedSessionKey,
            SetSessionKey = SharpScrobbler.SetSessionKey,
            OnTrackCanScrobble = SharpScrobbler.OnTrackCanScrobble,
            OnTrackStartsPlaying = SharpScrobbler.OnTrackStartsPlaying,
            OnTrackCompletes = SharpScrobbler.OnTrackCompletes,
        };
        ManagedExportsGCHandle = GCHandle.Alloc(ManagedExports);

        // ManagedExports is not blittable, so we need a copy.
        // Making a copy in unmanaged memory from the managed side ensures the GC will not mess with it (I hope)
        pManagedExports = Marshal.AllocHGlobal(Marshal.SizeOf(ManagedExports));
        Marshal.StructureToPtr(ManagedExports, pManagedExports, false);

        NativeImports.InitializeManagedExports(pManagedExports);

        return 0;
    }

    private static void FreeManagedExports()
    {
        Marshal.DestroyStructure<ManagedExports>(pManagedExports);
        Marshal.FreeHGlobal(pManagedExports);

        ManagedExportsGCHandle.Free();
        SharpScrobblerGCHandle.Free();

        SharpScrobbler?.Dispose();
        SharpScrobbler = null;
        SharpScrobblerGCHandle = default;
        ManagedExports = null;
        ManagedExportsGCHandle = default;
        pManagedExports = default;
    }

    private static void LogInfo(string text) => Logger.Log(LogLevel.Info, text);
    private static void LogWarning(string text) => Logger.Log(LogLevel.Warn, text);
    private static void LogVerbose(string text) => Logger.Log(LogLevel.Vrbs, text);
}
