using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace REstomp
{
    public class StompStreamParser
    {
        public static readonly string[] SupportedCommands = {
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

        protected Stream Stream { get; set; }

        public StompStreamParser(Stream stream)
        {
            Stream = stream;
        }

        /// <summary>
        /// Reads a STOMP command string asynchronyously.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>CommandString if read; otherwise null</returns>
        public async Task<string> ReadStompCommand(CancellationToken cancellationToken)
        {
            string commandString = null;

            //Create a new buffer, maximum possible allowed bytes in the line is 13
            var commandBuffer = new byte[13];

            //EOL is the line ending as defined in the spec of STOMP 1.2
            //EOL can either be CRLF or LF alone
            var eolIndex = -1;
            var bytesRead = 0;

            //while we haven't maxed out our buffer and we haven't found an EOL sequence
            while (bytesRead < commandBuffer.Length && eolIndex == -1)
            {
                //move forward in the buffer
                var offset = bytesRead;

                //Reading 1 byte at a time for now, may change in a future implementation
                const int length = 1;
                var bytesFound = 0;

                bytesFound += await Stream.ReadAsync(commandBuffer, offset, length, cancellationToken);

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
                var parsedCommandString = Encoding.UTF8.GetString(commandBuffer, 0, eolIndex);

                if (SupportedCommands.Contains(parsedCommandString))
                    commandString = parsedCommandString;
            }

            if(commandString == null) throw new CommandStringParseException();

            return commandString;
        }
    }
}