using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Xunit;

namespace REstomp.Test
{
    public class ParserTests
    {

        //CROSSJOIN of commands and EOL options
        public static IEnumerable<object[]> CommandData =>
            from command in new[]
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
            }
            from eol in new[]
            {
                "\n",
                "\r\n"
            }
            select new object[] { command, eol };

        public static IEnumerable<object[]> LowerCaseCommandData =>

            CommandData.Select(objects => new[]
            {
                ((string) objects[0]).ToLower(), objects[1]
            });

        [Theory(DisplayName = "Command Parses"),
            MemberData(nameof(CommandData))]
        [Trait("Category", "Parser")]

        public async void CommandParser(string command, string eol)
        {
            using (var memStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memStream))
            {
                streamWriter.Write(command);
                streamWriter.Write(eol);
                streamWriter.Write("version:1.2");
                streamWriter.Write(eol);
                streamWriter.Write(eol);
                streamWriter.Write(0x00);
                streamWriter.Flush();

                memStream.Position = 0;

                var parsedCommand = await StompStreamParser.ReadStompCommand(memStream, CancellationToken.None);

                Assert.StrictEqual(command, parsedCommand.Item2.CommandString);
            }
        }

        [Theory(DisplayName = "Command Parser is case-sensitive"),
            MemberData(nameof(LowerCaseCommandData))]
        [Trait("Category", "Parser")]
        public async void CommandParserIsCaseSensitive(string command, string eol)
        {
            await Assert.ThrowsAsync<CommandStringParseException>(async () =>
            {
                using (var memStream = new MemoryStream())
                using (var streamWriter = new StreamWriter(memStream))
                {
                    streamWriter.Write(command);
                    streamWriter.Write(eol);
                    streamWriter.Write("version:1.2");
                    streamWriter.Write(eol);
                    streamWriter.Write(eol);
                    streamWriter.Write(0x00);
                    streamWriter.Flush();

                    memStream.Position = 0;

                    await StompStreamParser.ReadStompCommand(memStream, CancellationToken.None);
                }
            });
        }
    }
}