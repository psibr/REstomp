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
        protected IEnumerable<byte> PrependedBytes = new List<byte>();

        public PrependableStream(TStream baseStream)
        {
            if (baseStream == null)
                throw new ArgumentNullException(nameof(baseStream));
            if (!baseStream.CanRead)
                throw new ArgumentException("baseStream must be readable", nameof(baseStream));

            BaseStream = baseStream;
        }

        /// <summary>
        /// Add any set of bytes the the start of the stream.
        /// </summary>
        /// <param name="prependBytes"></param>
        /// <param name="count"></param>
        public virtual void Prepend(ICollection<byte> prependBytes, int count)
        {
            Prepend(prependBytes.Take(count));
        }

        /// <summary>
        /// Add any set of bytes the the start of the stream.
        /// </summary>
        /// <param name="prependBytes"></param>
        public virtual void Prepend(ICollection<byte> prependBytes)
        {
            Prepend((IEnumerable<byte>)prependBytes);
        }

        /// <summary>
        /// Add any set of bytes the the start of the stream.
        /// </summary>
        /// <param name="prependBytes"></param>
        protected virtual void Prepend(IEnumerable<byte> prependBytes)
        {
            PrependedBytes = prependBytes.Union(PrependedBytes).ToList();
        }

        public override bool CanRead =>
            BaseStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length =>
            BaseStream.Length + PrependedBytes.Count();

        public override long Position
        {
            get { return BaseStream.Position - PrependedBytes.Count(); }

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

            if (PrependedBytes.Any())
            {
                var i = 0;
                foreach (var prependedByte in PrependedBytes)
                {
                    if (i >= count) break;

                    buffer[offset + i] = prependedByte;

                    i++;
                    prependedBytesRead++;

                }

                bytesRead += prependedBytesRead;
            }
            else
            {
                bytesRead += BaseStream.Read(buffer, offset, count);
            }

            PrependedBytes = PrependedBytes.Skip(prependedBytesRead).ToList();

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

        /// <summary>
        /// Returns the original stream. Does NOT include prepended bytes.
        /// </summary>
        /// <returns>The base stream.</returns>
        public TStream Unwrap() =>
            BaseStream;
    }
}