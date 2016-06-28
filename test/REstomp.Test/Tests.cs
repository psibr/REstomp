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
            CommandData.Select(objects => new []
            {
                (objects[0] as string).ToLower(), objects[1]
            });

        [Theory(DisplayName = "Command Parses"),
            MemberData("CommandData")]
        [Trait("Category", "Parser")]
        public void CommandParser(string command, string eol)
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

                var parser = new StompStreamParser(memStream);

                var parsedCommand = parser.ReadStompCommand(CancellationToken.None).Result;

                Assert.StrictEqual(command, parsedCommand);
            }
        }

        [Theory(DisplayName = "Command Parser is case-sensitive"),
            MemberData("LowerCaseCommandData")]
        [Trait("Category", "Parser")]
        public void CommandParserIsCaseSensitive(string command, string eol)
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

                var parser = new StompStreamParser(memStream);

                var parsedCommand = parser.ReadStompCommand(CancellationToken.None).Result;

                Assert.Equal(null, parsedCommand);
            }
        }
    }
}