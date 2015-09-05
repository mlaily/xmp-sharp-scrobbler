using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Scrobbling
{
    public class ApiError
    {
        public int Code { get; }
        public string Message { get; }
        public ApiError(int code, string message)
        {
            Code = code;
            Message = message;
        }
    }

    public class ApiResponse<T>
    {
        /// <summary>
        /// True if the response status is "ok", False if the response status is "failed".
        /// </summary>
        public bool Success { get; }
        /// <summary>
        /// Full response string as returned by the server.
        /// </summary>
        public string RawResponse { get; }
        /// <summary>
        /// Parsed response result.
        /// Will be null if Success is false.
        /// </summary>
        public T Result { get; }
        /// <summary>
        /// Parsed response error if the response status is "failed".
        /// Will be null if Success is true.
        /// </summary>
        public ApiError Error { get; }

        /// <param name="rawResponse">The full response string returned by the server.</param>
        /// <param name="parse">
        /// User defined parsing logic.
        /// Given the parsed lfm root node as an <see cref="XElement"/>,
        /// this function is expected to return an instance of <see cref="T"/>.</param>
        public ApiResponse(string rawResponse, Func<XElement, T> parse)
        {
            RawResponse = rawResponse;
            // parse the <lfm> wrapper node
            XElement lfm = XElement.Parse(rawResponse);
            var status = lfm.Attribute("status");
            if (status.Value == "ok")
            {
                Success = true;
                Result = parse(lfm);
            }
            else if (status.Value == "failed")
            {
                // <lfm status="failed">
                //    <error code="10">Invalid API Key</error>
                // </lfm>
                Success = false;
                var error = lfm.Element("error");
                int code = int.Parse(error.Attribute("code").Value);
                Error = new ApiError(code, error.Value);
            }
            else
            {
                throw new ArgumentException("The provided response is not a valid Last.fm API response!", nameof(rawResponse));
            }
        }
    }
}
