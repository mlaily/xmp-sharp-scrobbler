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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Scrobbling
{
    public static class Auth
    {
        public static Task<ApiResponse<string>> GetToken()
            => Common.GetAsync("auth.getToken", x => x.Value, addSignature: true);

        public static string GetAuthorizeTokenUrl(string token)
            => $"http://www.last.fm/api/auth?api_key={Common.ApiKey}&token={token}";

        public static Task<ApiResponse<Session>> GetSession(string authorizedToken)
            => Common.GetAsync("auth.getSession", x => new Session(x), addSignature: true, args: new ApiArg("token", authorizedToken));
    }

    public class Session
    {
        public string UserName { get; }
        public string Key { get; }
        public bool Subscriber { get; }

        public Session(XElement xml)
        {
            if (xml == null) throw new ArgumentNullException(nameof(xml));
            var sessionElement = xml.Element("session");
            UserName = sessionElement.Element("name").Value;
            Key = sessionElement.Element("key").Value;
            Subscriber = Util.ParseApiBool(sessionElement.Element("subscriber").Value);
        }
    }
}
