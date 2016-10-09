using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xmp_sharp_scrobbler_managed
{
    internal class Win32Window : IWin32Window
    {
        public IntPtr Handle { get; }
        public Win32Window(IntPtr handle)
        {
            Handle = handle;
        }
    }
}
