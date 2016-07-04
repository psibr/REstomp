using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace REstomp
{
    public class PrependableStream<TStream> : Stream
        where TStream : Stream
    {
        protected TStream BaseStream { get; }
        protected List<byte> PrependedBytes = new List<byte>();

        public PrependableStream(TStream baseStream)
        {
            if (baseStream == null)
                throw new ArgumentNullException(nameof(baseStream));
            if (!baseStream.CanRead)
                throw new ArgumentException("baseStream must be readable", nameof(baseStream));

            BaseStream = baseStream;
        }

        public virtual void Prepend(IEnumerable<byte> prependBytes)
        {
            PrependedBytes.AddRange(prependBytes);
        }

        public override bool CanRead =>
            BaseStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length =>
            BaseStream.Length + PrependedBytes.Count;

        public override long Position
        {
            get { return BaseStream.Position - PrependedBytes.Count; }

            set { throw new NotSupportedException(); }
        }

        public override void Flush() =>
            BaseStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "offset must not be negative");
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "count must be positive non-zero");
            if (offset + count > buffer.Length)
                throw new ArgumentException("The sum of offset and count is larger than the buffer length.");

            var bytesRead = 0;
            var prependedBytesRead = 0;

            if (PrependedBytes.Count > 0)
            {
                for (int i = 0; i < (count >= PrependedBytes.Count ? PrependedBytes.Count : count); i++)
                {
                    buffer[offset + i] = PrependedBytes.ElementAt(i);

                    prependedBytesRead++;
                }
            }

            bytesRead += prependedBytesRead;

            bytesRead += BaseStream.Read(buffer, offset + bytesRead, count - bytesRead);

            PrependedBytes.RemoveRange(0, prependedBytesRead);

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}