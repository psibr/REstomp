using System;
using System.Text;

namespace REstomp
{
    public static class StompHeaderExtensions
    {
        public static Encoding GetEncoding(this StompFrame frame)
        {
            var contentTypeHeader = frame.GetContentTypeHeader();

            if (contentTypeHeader == null)
                throw new ArgumentNullException(nameof(contentTypeHeader));

            return Encoding.GetEncoding(contentTypeHeader.Charset ?? "utf-8");
        }

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
            string charsetString = fragments.Length == 2 ? fragments[1] : null;
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

            return new ContentTypeHeader(contentType.Trim().ToLowerInvariant(), charset.Trim().ToLowerInvariant());
        }

        public static ContentLengthHeader GetContentLengthHeader(this StompFrame frame)
        {
            string contentLengthHeaderValue;
            if (!frame.Headers.TryGetValue("content-length", out contentLengthHeaderValue))
                return ContentLengthHeader.Empty;

            return new ContentLengthHeader(contentLengthHeaderValue);
        }
    }
}