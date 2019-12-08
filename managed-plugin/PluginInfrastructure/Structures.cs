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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XmpSharpScrobbler.Misc;

namespace XmpSharpScrobbler.PluginInfrastructure
{
#pragma warning disable CA1051 // Do not declare visible instance fields

    [StructLayout(LayoutKind.Sequential)]
    public class ScrobblerConfig
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] sessionKey = new byte[32];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public readonly byte[] userName = new byte[128];

        public string SessionKey
        {
            get => InteropHelper.GetStringFromNativeBuffer(sessionKey, Encoding.ASCII);
            set => InteropHelper.SetNativeString(sessionKey, Encoding.ASCII, value);
        }

        public string UserName
        {
            get => InteropHelper.GetStringFromNativeBuffer(userName, Encoding.UTF8);
            set => InteropHelper.SetNativeString(userName, Encoding.UTF8, value);
        }
    }
}
#pragma warning restore CA1051 // Do not declare visible instance fields
