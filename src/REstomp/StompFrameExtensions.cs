using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        public static StompFrame ReadFromEnvironmentRequest(this IDictionary<string, object> environment)
        {
            var method = environment["stomp.requestMethod"] as string;
            var headers = (ImmutableArray<KeyValuePair<string, string>>)environment["stomp.requestHeaders"];
            var body = (ImmutableArray<byte>)environment["stomp.requestBody"];

            if(string.IsNullOrWhiteSpace(method))
                return StompFrame.Empty;

            return new StompFrame(method, headers, body);
        }

        public static StompFrame ReadFromEnvironmentResponse(this IDictionary<string, object> environment)
        {
            StompFrame responseFrame = null;

            var command = environment["stomp.reponseMethod"] as string;

            if (!string.IsNullOrWhiteSpace(command) && StompParser.Command.IsSupported(command))
            {
                var headers = (ImmutableArray<KeyValuePair<string, string>>)environment["stomp.responseHeaders"];
                var body = (ImmutableArray<byte>)environment["stomp.responseBody"];

                responseFrame = new StompFrame(command, headers, body);
            }

            return responseFrame;
        }
    }
}