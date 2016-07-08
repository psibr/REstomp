using System;
using System.Text;

namespace REstomp
{
    public class ContentTypeHeader
    {
        public ContentTypeHeader(string contentType, string charSet = null)
        {
            ContentType = contentType;
            Charset = charSet;
        }

        private ContentTypeHeader() { }

        public string ContentType { get; }

        public string Charset { get; }

        public static ContentTypeHeader Empty { get; } = new ContentTypeHeader();
    }

    public static class ContentTypeHeaderExtensions
    {
        public static Encoding GetEncoding(this ContentTypeHeader contentTypeHeader)
        {
            if(contentTypeHeader == null)
                throw new ArgumentNullException(nameof(contentTypeHeader));

            return Encoding.GetEncoding(contentTypeHeader.Charset ?? "utf-8");
        }
    }
}