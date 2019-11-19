using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace xmp_sharp_scrobbler.PluginInfrastructure
{
    public static class Imports
    {
        public static void ShowInfoBubble(string text, TimeSpan? displayTime = null)
            => NativeImports.ShowInfoBubble(text, displayTime == null ? 0 : (int)displayTime.Value.TotalMilliseconds);
    }
}
