namespace REstomp
{
    public class ContentLengthHeader
    {
        public ContentLengthHeader(string contentLength)
        {
            int contentLengthParsed;
            if(!int.TryParse(contentLength, out contentLengthParsed))
                throw new ContentLengthException();

            ContentLength = contentLengthParsed;
        }

        private ContentLengthHeader() { }

        public int ContentLength { get; } = -1;

        public static ContentLengthHeader Empty { get; } = new ContentLengthHeader();
    }
}