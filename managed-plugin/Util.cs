using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xmp_sharp_scrobbler_managed
{
    public delegate void ShowInfoBubbleHandler(string text, int displayTimeMs);

    public static class Util
    {
        private static ShowInfoBubbleHandler _ShowInfoBubble;
        public static void InitializeShowBubbleInfo(ShowInfoBubbleHandler showInfoBubble)
            => _ShowInfoBubble = showInfoBubble;
        public static void ShowInfoBubble(string text, TimeSpan? displayTime = null)
            => _ShowInfoBubble?.Invoke(text, displayTime == null ? 0 : (int)displayTime.Value.TotalMilliseconds);
    }

    /// <summary>
    /// The object equivalent of void.
    /// </summary>
    public struct Unit { }
}
