using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace REstomp
{
    public static class StompParser
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
                new[] {SEND, MESSAGE, ERROR}.Contains(command);
        }

        /// <summary>
        /// Reads a STOMP command string asynchronyously.
        /// </summary>
        /// <typeparam name="TStream">The type of the stream.</typeparam>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="stompFrame">An existing stomp frame to use as a base.</param>
        /// <returns>A tuple of the Stream and the resultant StompFrame</returns>
        public static async Task<Tuple<TStream, StompFrame>> ReadStompCommand<TStream>(
            TStream stream, StompFrame stompFrame) where TStream : Stream
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
        public static async Task<Tuple<TStream, StompFrame>> ReadStompCommand<TStream>(
            TStream stream) where TStream : Stream
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
        public static async Task<Tuple<TStream, StompFrame>> ReadStompCommand<TStream>(
            TStream stream, CancellationToken cancellationToken) where TStream : Stream
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
        public static async Task<Tuple<TStream, StompFrame>> ReadStompCommand<TStream>(
            TStream stream, StompFrame stompFrame, CancellationToken cancellationToken)
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
                    try
                    {
                        stream.Position = originalStreamPosition;
                    }
                    catch (Exception)
                    {
                        // Nothing we can do here. Sad day.

                        //TODO: Could possibly create a StreamPrepender in this case?
                    }

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

            if (command == null) throw new CommandParseException();

            var newFrame = stompFrame
                .With(frame => frame.Command, command);

            return Tuple.Create(stream, newFrame);
        }
    }
}