using System;
using System.Collections.Generic;
using System.Text;

namespace REstomp
{
    public static class StompFrameExtensions
    {
        public static ContentTypeHeader GetContentTypeHeader(this StompFrame frame)
        {
            // application/json;charset=utf-8
            string headerValue;
            if (!frame.Headers.TryGetValue("content-type", out headerValue)
                || string.IsNullOrWhiteSpace(headerValue))
            {
                return ContentTypeHeader.Empty;
            }

            var fragments = headerValue.Split(';');

            var contentType = fragments[0];
            string charsetString = fragments.Length >= 2 ? fragments[1] : null;
            string charset = null;
            if (charsetString != null)
            {
                const string charSetIdentifier = "charset=";
                var index = charsetString.IndexOf(charSetIdentifier);

                if (index > -1)
                {
                    charset = charsetString.Substring(index + charSetIdentifier.Length, charsetString.Length - (charSetIdentifier.Length + index));
                }
            }

            return new ContentTypeHeader(contentType.Trim().ToLowerInvariant(), charset?.Trim().ToLowerInvariant());
        }

        public static ContentLengthHeader GetContentLengthHeader(this StompFrame frame)
        {
            string contentLengthHeaderValue;
            if (!frame.Headers.TryGetValue("content-length", out contentLengthHeaderValue))
                return ContentLengthHeader.Empty;

            return new ContentLengthHeader(contentLengthHeaderValue);
        }

        public static IDictionary<string, object> WriteToEnvironmentRequest(this StompFrame frame, IDictionary<string, object> environment)
        {
            environment["stomp.requestMethod"] = frame.Command;
            environment["stomp.requestHeaders"] = frame.Headers;
            environment["stomp.requestBody"] = frame.Body;

            return environment;
        }

        public static IDictionary<string, object> WriteToEnvironmentResponse(this StompFrame frame, IDictionary<string, object> environment)
        {
            environment["stomp.responseMethod"] = frame.Command;
            environment["stomp.responseHeaders"] = frame.Headers;
            environment["stomp.responseBody"] = frame.Body;

            return environment;
        }
    }
}