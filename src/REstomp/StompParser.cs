using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace REstomp
{
    public class StompParser : IStompParser
    {
        public static class Command
        {
            private static readonly string[] SupportedCommands =
            {
                "STOMP",
                "CONNECT",
                "CONNECTED",
                "ERROR",
                "SEND",
                "SUBSCRIBE",
                "UNSUBSCRIBE",
                "MESSAGE",
                "ACK",
                "NACK",
                "BEGIN",
                "COMMIT",
                "ABORT",
                "DISCONNECT",
                "RECEIPT"
            };

            // ReSharper disable InconsistentNaming

            public static string STOMP { get; } = nameof(STOMP);
            public static string CONNECT { get; } = nameof(CONNECT);
            public static string CONNECTED { get; } = nameof(CONNECTED);
            public static string ERROR { get; } = nameof(ERROR);
            public static string SEND { get; } = nameof(SEND);
            public static string SUBSCRIBE { get; } = nameof(SUBSCRIBE);
            public static string UNSUBSCRIBE { get; } = nameof(UNSUBSCRIBE);
            public static string MESSAGE { get; } = nameof(MESSAGE);
            public static string ACK { get; } = nameof(ACK);
            public static string NACK { get; } = nameof(NACK);
            public static string BEGIN { get; } = nameof(BEGIN);
            public static string COMMIT { get; } = nameof(COMMIT);
            public static string ABORT { get; } = nameof(ABORT);
            public static string DISCONNECT { get; } = nameof(DISCONNECT);
            public static string RECEIPT { get; } = nameof(RECEIPT);

            // ReSharper restore InconsistentNaming

            public static bool IsSupported(string command) =>
                SupportedCommands.Contains(command);

            public static bool CanHaveBody(string command) =>
                new[] { SEND, MESSAGE, ERROR }.Contains(command);
        }

        /// <summary>
        /// Reads a STOMP command string asynchronyously.
        /// </summary>
        /// <typeparam name="TStream">The type of the stream.</typeparam>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="stompFrame">An existing stomp frame to use as a base.</param>
        /// <returns>A tuple of the Stream and the resultant StompFrame</returns>
        public static async Task<Tuple<PrependableStream<TStream>, StompFrame>> ReadStompCommand<TStream>(
            PrependableStream<TStream> stream, StompFrame stompFrame) where TStream : Stream
        {
            return await ReadStompCommand(stream, stompFrame, CancellationToken.None)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Reads a STOMP command string asynchronyously.
        /// </summary>
        /// <typeparam name="TStream">The type of the stream.</typeparam>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>A tuple of the Stream and the resultant StompFrame</returns>
        public static async Task<Tuple<PrependableStream<TStream>, StompFrame>> ReadStompCommand<TStream>(
            PrependableStream<TStream> stream) where TStream : Stream
        {
            return await ReadStompCommand(stream, StompFrame.Empty, CancellationToken.None)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Reads a STOMP command string asynchronyously.
        /// </summary>
        /// <typeparam name="TStream">The type of the stream.</typeparam>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A tuple of the Stream and the resultant StompFrame</returns>
        public static async Task<Tuple<PrependableStream<TStream>, StompFrame>> ReadStompCommand<TStream>(
            PrependableStream<TStream> stream, CancellationToken cancellationToken) where TStream : Stream
        {
            return await ReadStompCommand(stream, StompFrame.Empty, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Reads a STOMP command string asynchronyously.
        /// </summary>
        /// <typeparam name="TStream">The type of the stream.</typeparam>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="stompFrame">An existing stomp frame to use as a base.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A tuple of the Stream and the resultant StompFrame</returns>
        /// <exception cref="CommandParseException"></exception>
        public static async Task<Tuple<PrependableStream<TStream>, StompFrame>> ReadStompCommand<TStream>(
            PrependableStream<TStream> stream, StompFrame stompFrame, CancellationToken cancellationToken)
            where TStream : Stream
        {
            var originalStreamPosition = stream.Position;
            string command = null;

            //Create a new buffer, maximum possible allowed bytes in the line is 13
            var commandBuffer = new byte[13];

            //EOL is the line ending as defined in the spec of STOMP 1.2
            //EOL can either be CRLF or LF alone
            var eolIndex = -1;
            var bytesRead = 0;

            //while we haven't maxed out our buffer and we haven't found an EOL sequence
            while (bytesRead < commandBuffer.Length && eolIndex == -1)
            {

                //Check for cancellation and attempt to handle it gracefully.
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    stream.Prepend(commandBuffer, bytesRead);

                    throw;
                }

                //move forward in the buffer
                var offset = bytesRead;

                //Reading 1 byte at a time for now, may change in a future implementation
                const int length = 1;
                var bytesFound = 0;

                bytesFound += await stream.ReadAsync(commandBuffer, offset, length, cancellationToken);

                //check for EOL in the bytes we read. (1 iteration since length is const of 1)
                for (var i = offset; i < offset + bytesFound; i++)
                {
                    if (commandBuffer[i] == 0x0a)
                        eolIndex = i;
                }

                bytesRead += bytesFound;
            }

            //if we have a potentially meaningful line (STOMPs shortest command is 3 characters)
            if (eolIndex > 2)
            {
                //If last byte before LF was CR, move the EOL start index back one
                if (commandBuffer[eolIndex - 1] == 0x0d)
                {
                    eolIndex--;
                }

                //Convert bytes to string in UTF-8 from beginning to start of EOL
                var parsedCommand = Encoding.UTF8.GetString(commandBuffer, 0, eolIndex);

                if (Command.IsSupported(parsedCommand))
                    command = parsedCommand;
            }

            if (command == null)
            {
                //add the bytes that we read for a potential failsafe
                stream.Prepend(commandBuffer, bytesRead);

                throw new CommandParseException();
            }

            var newFrame = stompFrame
                .With(frame => frame.Command, command);

            return Tuple.Create(stream, newFrame);
        }


        /// <summary>
        /// Reads a STOMP header block asynchronyously.
        /// </summary>
        /// <typeparam name="TStream">The type of the stream.</typeparam>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>A tuple of the Stream, resultant StompFrame, and any remainder bytes to be parsed in the body.</returns>
        /// <exception cref="HeaderParseException"></exception>
        public static async Task<Tuple<PrependableStream<TStream>, StompFrame>> ReadStompHeaders<TStream>(
            PrependableStream<TStream> stream) where TStream : Stream
        {
            return await ReadStompHeaders(stream, StompFrame.Empty, CancellationToken.None)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Reads a STOMP header block asynchronyously.
        /// </summary>
        /// <typeparam name="TStream">The type of the stream.</typeparam>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="stompFrame">An existing stomp frame to use as a base.</param>
        /// <returns>A tuple of the Stream, resultant StompFrame, and any remainder bytes to be parsed in the body.</returns>
        /// <exception cref="HeaderParseException"></exception>
        public static async Task<Tuple<PrependableStream<TStream>, StompFrame>> ReadStompHeaders<TStream>(
            PrependableStream<TStream> stream, StompFrame stompFrame) where TStream : Stream
        {
            return await ReadStompHeaders(stream, stompFrame, CancellationToken.None)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Reads a STOMP header block asynchronyously.
        /// </summary>
        /// <typeparam name="TStream">The type of the stream.</typeparam>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A tuple of the Stream, resultant StompFrame, and any remainder bytes to be parsed in the body.</returns>
        /// <exception cref="HeaderParseException"></exception>
        public static async Task<Tuple<PrependableStream<TStream>, StompFrame>> ReadStompHeaders<TStream>(
            PrependableStream<TStream> stream, CancellationToken cancellationToken) 
            where TStream : Stream
        {
            return await ReadStompHeaders(stream, StompFrame.Empty, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Reads a STOMP header block asynchronyously.
        /// </summary>
        /// <typeparam name="TStream">The type of the stream.</typeparam>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="stompFrame">An existing stomp frame to use as a base.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A tuple of the Stream, resultant StompFrame, and any remainder bytes to be parsed in the body.</returns>
        /// <exception cref="HeaderParseException"></exception>
        public static async Task<Tuple<PrependableStream<TStream>, StompFrame>> ReadStompHeaders<TStream>(
            PrependableStream<TStream> stream, StompFrame stompFrame, CancellationToken cancellationToken)
            where TStream : Stream
        {
            //put our headers with values into a dictionary after parsing them
            var headers = new List<KeyValuePair<string,string>>();

            var headerBuffer = new byte[20];
            var bytesFound = 0;

            //use eolsEncountered to determine when we move out of the headers and into the body
            var eolsEncountered = 0;

            //a single header line (before colon is parsed)
            var headerLine = new List<byte>();

            //a list of header lines
            var headerContents = new List<List<byte>>();

            //index of the next character we wish to read
            var parserIndex = 0;

            while (true)
            {
                //Check for cancellation and attempt to handle it gracefully.
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    var relevantBytes = new byte[bytesFound - parserIndex];
                    Buffer.BlockCopy(headerBuffer, parserIndex, relevantBytes, 0,
                        bytesFound - parserIndex);

                    //get ALLLLLLLL of the bytes we've read and put them in an array
                    var readBytes = headerContents
                        .SelectMany(list => list)
                        .Union(headerLine)
                        .Union(relevantBytes)
                        .ToArray();

                    //add the bytes that we read for a potential failsafe
                    stream.Prepend(readBytes);

                    throw;
                }

                //if this message does not contain more headers
                //(that is, if we encountered two EOLs in a row)
                if (eolsEncountered > 1) break;

                //add more bytes if we got the entire way through our last netStream.Read
                if (parserIndex == bytesFound)
                {
                    parserIndex = 0;

                    bytesFound = await stream.ReadAsync(headerBuffer, 0, headerBuffer.Length, cancellationToken);
                }

                for (var i = parserIndex; i < bytesFound; i++)
                {
                    parserIndex = i + 1;

                    //if this byte is LF
                    if (headerBuffer[i] == 0x0a)
                    {

                        //add the line to contents and set eol to encountered
                        if (headerLine.Any()) headerContents.Add(headerLine);
                        headerLine = new List<byte>();

                        eolsEncountered++;
                        break;

                    }
                    //if the byte is not LF

                    //if the character is not CR
                    if (headerBuffer[i] != 0x0d)
                    {
                        //add the character and set eolEncountered to false
                        headerLine.Add(headerBuffer[i]);
                        eolsEncountered = 0;
                    }
                }
            }

            //split the headers and values
            var headerSegments = headerContents.Select(line =>
                        Encoding.UTF8.GetString(line.ToArray()).Split(':')).ToList();

            //throw an exception if a header is null or white space or if a value is null
            if (headerSegments.Any(segment =>
                segment.Length != 2 
                    || string.IsNullOrWhiteSpace(segment[0])))
            {
                var relevantBytes = new byte[bytesFound - parserIndex];
                Buffer.BlockCopy(headerBuffer, parserIndex, relevantBytes, 0,
                    bytesFound - parserIndex);

                //get ALLLLLLLL of the bytes we've found and put them in an array
                var readBytes = headerContents
                        .SelectMany(list => list)
                        .Union(headerLine)
                        .Union(relevantBytes)
                        .ToArray();

                stream.Prepend(readBytes);
                throw new HeaderParseException();
            }

            //Add headers to the list
            foreach (var segments in headerSegments)
            {
                var key = segments[0];
                var value = segments[1];
                var newPair = new KeyValuePair<string, string>(key, value);
                headers.Add(newPair);
            }

            //create a Stomp Frame with headers
            var frameWithHeaders = stompFrame.With(frame => frame.Headers, headers.ToImmutableArray());

            //find the remaining bytes and put them at the front of an array so we don't lose part of the body
            var bodyBuffer = new byte[bytesFound - parserIndex];
            Buffer.BlockCopy(headerBuffer, parserIndex, bodyBuffer, 0, bytesFound - parserIndex);

            stream.Prepend(bodyBuffer);

            return Tuple.Create(stream, frameWithHeaders);
        }

        //TODO: add overloads

        /// <summary>
        /// Reads a STOMP body block asynchronyously.
        /// </summary>
        /// <typeparam name="TStream">The type of the stream.</typeparam>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="stompFrame">An existing stomp frame to use as a base.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A tuple of the Stream and the resultant StompFrame.</returns>
        /// <exception cref="HeaderParseException"></exception>
        public static async Task<Tuple<PrependableStream<TStream>, StompFrame>> ReadStompBody<TStream>(
            PrependableStream<TStream> stream, StompFrame stompFrame, CancellationToken cancellationToken)
            where TStream : Stream
        {
            //list of bytes that we read
            var bodyList = new List<byte>();

            //if we have a content-length header, we MUST read the number of bytes that the value specifies
            var contentLength = -1;
            var bodyBytesRead = 0;

            //Attempt to find and use the content-length header.
            string contentLengthHeaderValue;
            if (stompFrame.Headers.TryGetValue("content-length", out contentLengthHeaderValue))
                if (!int.TryParse(contentLengthHeaderValue, out contentLength))
                    throw new ContentLengthException(); //ERROR frame, content-length not assignable to int

            //if we have content length, read that many bytes. the following byte must be 0x00
            if (contentLength != -1)
            {
                //grab the remaining body and the following 0x00 null byte
                var bodyLengthBuffer = new byte[contentLength - bodyBytesRead + 1];
                var bytesFound = 0;
                var parserIndex = 0;

                //read from stream and add to bodyList until we've read the remaining body and 0x00 byte
                while (bodyBytesRead < contentLength + 1)
                {
                    //Check for cancellation and attempt to handle it gracefully.
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    catch (OperationCanceledException)
                    {

                        //add the bytes that we read for a potential failsafe
                        stream.Prepend(bodyList);

                        throw;
                    }

                    bytesFound += await stream.ReadAsync(bodyLengthBuffer, bytesFound, bodyLengthBuffer.Length - bytesFound, cancellationToken);

                    for (var i = parserIndex; i < bytesFound; i++)
                    {
                        bodyList.Add(bodyLengthBuffer[i]);
                    }
                    bodyBytesRead = bytesFound;
                    parserIndex = bytesFound;
                }

                //if the last byte wasn't actually 0x00, throw an exception
                if (bodyList.Last() != 0x00)
                    throw new ContentLengthException();
            }
            else
            {
                var bodyBuffer = new byte[20];
                
                //if we don't have a content length, just read until we find a 0x00 null byte
                while (!bodyList.Any() || bodyList.Last() != 0x00)
                {
                    //Check for cancellation and attempt to handle it gracefully.
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    catch (OperationCanceledException)
                    {
                        //add the bytes that we read for a potential failsafe
                        stream.Prepend(bodyList);

                        throw;
                    }

                    var bytesFound = await stream.ReadAsync(bodyBuffer, 0, bodyBuffer.Length, cancellationToken);

                    for (int i = 0; i < bytesFound; i++)
                    {
                        bodyList.Add(bodyBuffer[i]);

                        if(bodyBuffer[i] == 0x00)
                            break;
                    }
                }
            }

            //remove the trailing 0x00
            bodyList.RemoveAt(bodyList.Count - 1);

            var newFrame = stompFrame
                .With(frame => frame.Body, bodyList.ToImmutableArray());

            return Tuple.Create(stream, newFrame);
        }


        public async Task<Tuple<TStream, StompFrame>> ReadStompFrame<TStream>(TStream stream, CancellationToken cancellationToken) 
            where TStream : Stream
        {
            var result = await stream.AsPrependableStream()
                .ReadStompCommand(cancellationToken)
                .ThenReadStompHeaders()
                .ThenReadStompBody();

            return Tuple.Create(result.Item1.Unwrap(), result.Item2);
        }

        public async Task<Tuple<TStream, StompFrame>> ReadStompFrame<TStream>(TStream stream) where TStream : Stream
        {
            return await ReadStompFrame(stream, CancellationToken.None)
                .ConfigureAwait(false);
        }

        public async Task<StompFrame> ReadStompFrame(string value, CancellationToken cancellationToken)
        {
            using (var memStream = new MemoryStream(Encoding.UTF8.GetBytes(value)))
            {
                return (await ReadStompFrame(memStream, cancellationToken)).Item2;
            }
        }

        public async Task<StompFrame> ReadStompFrame(string value)
        {
            return await ReadStompFrame(value, CancellationToken.None)
                .ConfigureAwait(false);
        }
    }

    public interface IStompParser
    {
        Task<Tuple<TStream, StompFrame>> ReadStompFrame<TStream>(TStream stream, CancellationToken cancellationToken)
            where TStream : Stream;

        Task<Tuple<TStream, StompFrame>> ReadStompFrame<TStream>(TStream stream)
            where TStream : Stream;

        Task<StompFrame> ReadStompFrame(string value, CancellationToken cancellationToken);

        Task<StompFrame> ReadStompFrame(string value);
    }
}