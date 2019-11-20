using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace XmpSharpScrobbler.PluginInfrastructure
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "I don't care about VB.")]
    public static class Imports
    {
        public static void ShowInfoBubble(string text, TimeSpan? displayTime = null)
            => NativeImports.ShowInfoBubble(text, displayTime == null ? 0 : (int)displayTime.Value.TotalMilliseconds);
    }
}
