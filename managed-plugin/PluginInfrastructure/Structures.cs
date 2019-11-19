using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace xmp_sharp_scrobbler.PluginInfrastructure
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct TrackInfo
    {
        // Just an integer (not a pointer), but its size is platform dependant...
        public IntPtr playStartTimestamp;

        public string title;
        public string artist;
        public string album;
        public string trackNumber;
        public string mbid;
    }
}
