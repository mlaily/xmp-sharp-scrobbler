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
        public static Task<ApiResult<string>> GetToken()
        {
            var requestString = Common.CreateRequestString("auth.getToken", addSignature: true);
            return Common.GetAsync(requestString, x => x.Value);
        }

        public static string GetAuthorizeTokenUrl(string token)
            => $"http://www.last.fm/api/auth?api_key={Common.ApiKey}&token={token}";

        public static Task<ApiResult<Session>> GetSession(string authorizedToken)
        {
            var requestString = Common.CreateRequestString("auth.getSession", addSignature: true, args: new ApiArg("token", authorizedToken));
            return Common.GetAsync(requestString, x => new Session(x));
        }
    }

    public class Session
    {
        public string UserName { get; }
        public string Key { get; }
        public int Subscriber { get; }

        public Session(XElement xml)
        {
            var sessionElement = xml.Element("session");
            UserName = sessionElement.Element("name").Value;
            Key = sessionElement.Element("key").Value;
            Subscriber = int.Parse(sessionElement.Element("subscriber").Value);
        }
    }
}
