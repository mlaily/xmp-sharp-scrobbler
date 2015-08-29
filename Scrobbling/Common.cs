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


        /// <summary>
        /// Execute a GET request.
        /// Signature is optional, depending on the provided method.
        /// Refer to the API documentation for more information.
        /// </summary>
        /// <param name="parse">
        /// User defined parsing logic.
        /// Given the parsed lfm root node as an <see cref="XElement"/>,
        /// this function is expected to return an instance of <see cref="T"/>.
        /// </param>
        public static async Task<ApiResult<T>> GetAsync<T>(string method, Func<XElement, T> parse, bool addSignature, params ApiArg[] args)
        {
            var requestString = CreateRequestString(
                method: method,
                sessionKey: null,
                addSignature: addSignature,
                includeBaseAddress: true,
                args: args);
            using (var response = await HttpClient.GetAsync(requestString))
            {
                var body = await response.Content.ReadAsStringAsync();
                return new ApiResult<T>(body, parse);
            }
        }

        /// <summary>
        /// Execute a POST request.
        /// POST requests are always authenticated, so a session key is required.
        /// </summary>
        /// <param name="parse">
        /// User defined parsing logic.
        /// Given the parsed lfm root node as an <see cref="XElement"/>,
        /// this function is expected to return an instance of <see cref="T"/>.
        /// </param>
        public static async Task<ApiResult<T>> PostAsync<T>(string method, string sessionKey, Func<XElement, T> parse, params ApiArg[] args)
        {
            var requestString = CreateRequestString(
                method: method,
                sessionKey: sessionKey,
                addSignature: true,
                includeBaseAddress: false,
                args: args);
            var buffer = Encoding.UTF8.GetBytes(requestString);
            using (var response = await HttpClient.PostAsync(ApiBaseAddress, new ByteArrayContent(buffer)))
            {
                var body = await response.Content.ReadAsStringAsync();
                return new ApiResult<T>(body, parse);
            }
        }

        private static string CreateRequestString(string method, string sessionKey, bool addSignature, bool includeBaseAddress, params ApiArg[] args)
        {
            // get all the arguments, taking the signature into account if required
            var allUnsignedArgs = (args ?? Enumerable.Empty<ApiArg>()).Concat(new[]
            {
                new ApiArg("method", method),
                new ApiArg("api_key", ApiKey),
            });
            // append the session key only if it is actually provided
            if (sessionKey != null)
                allUnsignedArgs = allUnsignedArgs.Append(new ApiArg("sk", sessionKey));

            IEnumerable<ApiArg> finalArgs;
            // the signature is based on all the args except the signature (obviously)
            if (addSignature)
                finalArgs = allUnsignedArgs.Append(GetSignature(allUnsignedArgs));
            else
                finalArgs = allUnsignedArgs;

            // create the request string
            var sb = new StringBuilder();
            bool firstArg = true;
            if (includeBaseAddress)
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
