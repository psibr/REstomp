using System.IO;

namespace REstomp
{
    public static class PrependableStreamExtensions
    {
        public static PrependableStream<TStream> AsPrependableStream<TStream>(this TStream stream)
            where TStream : Stream
        {


            return new PrependableStream<TStream>(stream);
        }
    }
}