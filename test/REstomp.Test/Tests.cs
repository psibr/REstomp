using System.Collections.Generic;
using System.IO;
using System.Linq;
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

                var parsedCommand = await StompParser.ReadStompCommand(memStream);

                Assert.StrictEqual(command, parsedCommand.Item2.Command);
            }
        }

        [Theory(DisplayName = "Command Parser is case-sensitive"),
            MemberData(nameof(LowerCaseCommandData))]
        [Trait("Category", "Parser")]
        public async void CommandParserIsCaseSensitive(string command, string eol)
        {
            await Assert.ThrowsAsync<CommandParseException>(async () =>
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

                    await StompParser.ReadStompCommand(memStream);
                }
            });
        }

        [Fact(DisplayName = "StompFrame With Command")]
        public void StompFrameWithCommand()
        {
            var expectation = new StompFrame(StompParser.Commands.CONNECT);

            var newFrame = StompFrame.Empty
                .With(frame => frame.Command, StompParser.Commands.CONNECT);

            Assert.NotNull(newFrame);
            Assert.Equal(expectation.Command, newFrame.Command);

            Assert.Empty(newFrame.Headers);
            Assert.True(newFrame.Body.IsDefault);
        }
    }
}