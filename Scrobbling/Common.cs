using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Scrobbling
{
    public static class Common
    {
        private const string ApiBaseAddress = "http://ws.audioscrobbler.com/2.0/";

        public const string ApiKey = "e5d0abaff24f1f12261ab21930c49499";
        private const string ApiSecret = "dcd2bf56f2f163fd5ae17af96a5299c1";

        private static HttpClient HttpClient { get; }

        static Common()
        {
            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.ExpectContinue = false;
        }


        /// <param name="parse">
        /// User defined parsing logic.
        /// Given the parsed lfm root node as an <see cref="XElement"/>,
        /// this function is expected to return an instance of <see cref="T"/>.
        /// </param>
        public static async Task<ApiResult<T>> GetAsync<T>(string requestString, Func<XElement, T> parse)
        {
            using (var response = await HttpClient.GetAsync(requestString))
            {
                var body = await response.Content.ReadAsStringAsync();
                return new ApiResult<T>(body, parse);
            }
        }

        public static string CreateRequestString(string method, bool addSignature, params ApiArg[] args)
        {
            var methodArg = new ApiArg("method", method);
            var apiKeyArg = new ApiArg("api_key", ApiKey);

            // get all the arguments, taking the signature into account if required
            var allUnsignedArgs = (args ?? Enumerable.Empty<ApiArg>()).Concat(new[] { methodArg, apiKeyArg });
            var finalArgs = allUnsignedArgs;
            if (addSignature) finalArgs = allUnsignedArgs.Concat(new[] { GetSignature(allUnsignedArgs) });

            // create the request string
            var sb = new StringBuilder();
            bool firstArg = true;
            sb.Append($"{ApiBaseAddress}?");
            foreach (var arg in finalArgs)
            {
                if (firstArg == false)
                {
                    sb.Append("&");
                }
                sb.Append($"{arg.Name}={arg.Value}");
                firstArg = false;
            }
            return sb.ToString();
        }

        private static ApiArg GetSignature(IEnumerable<ApiArg> args)
        {
            var sb = new StringBuilder();
            // order all the arguments alphabetically and concatenate them.
            foreach (var arg in args.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                sb.Append(arg.Name);
                sb.Append(arg.Value);
            }
            // append the secret
            sb.Append(ApiSecret);

            // calculate the md5 hash of the UTF-8 encoded resulting string
            var md5 = MD5.Create();
            var buffer = Encoding.UTF8.GetBytes(sb.ToString());
            var md5Result = md5.ComputeHash(buffer);

            // format the result hash to an hexadecimal string
            var sbMd5 = new StringBuilder(capacity: md5.HashSize / 8);
            foreach (byte b in md5Result)
            {
                sbMd5.AppendFormat("{0:x2}", b);
            }

            return new ApiArg("api_sig", sbMd5.ToString());
        }
    }

    public class ApiArg
    {
        public string Name { get; }
        public string Value { get; }

        public ApiArg(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
