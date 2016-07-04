using System.Collections.Generic;
using System.Collections.Immutable;
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
            var expectation = new StompFrame(StompParser.Command.CONNECT);

            var newFrame = StompFrame.Empty
                .With(frame => frame.Command, StompParser.Command.CONNECT);

            Assert.NotNull(newFrame);
            Assert.Equal(expectation.Command, newFrame.Command);

            Assert.Empty(newFrame.Headers);
            Assert.True(newFrame.Body.IsDefault);
        }

        [Theory(DisplayName = "StompFrame With Headers")]
        [InlineData("content-length", "126")]
        public void StompFrameWithHeaders(string headerKey, string headerValue)
        {
            var expectation = new StompFrame(StompParser.Command.MESSAGE, 
                new Dictionary<string, string> { {headerKey, headerValue} });

            var newFrame = new StompFrame(StompParser.Command.MESSAGE)
                .With(frame => frame.Headers, new Dictionary<string, string> { {headerKey, headerValue} }.ToImmutableDictionary());

            Assert.NotNull(newFrame);
            Assert.Equal(expectation.Command, newFrame.Command);

            Assert.Equal(expectation.Headers, newFrame.Headers);
            Assert.True(newFrame.Body.IsDefault);
        }

        [Fact(DisplayName = "Headers Parse")]
        public async void StompParseHeaders()
        {
            var command = StompParser.Command.MESSAGE;
            var headers = new Dictionary<string, string> { {"content-length", "126"} };

            var expectation = new StompFrame(command, headers);

            using (var memStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memStream))
            {
                var eol = "\r\n";
                streamWriter.Write(command);
                streamWriter.Write(eol);
                foreach (var header in headers)
                {
                    streamWriter.Write($"{header.Key}:{header.Value}");
                }
                streamWriter.Write(eol);
                streamWriter.Write(eol);
                streamWriter.Write(0x00);
                streamWriter.Flush();

                memStream.Position = 0;

                var parsedCommand = await StompParser.ReadStompCommand(memStream);
                var parsedHeaders = await StompParser.ReadStompHeaders(parsedCommand.Item1, parsedCommand.Item2);

                Assert.StrictEqual(expectation.Command, parsedCommand.Item2.Command);
                Assert.Equal(expectation.Headers, parsedHeaders.Item2.Headers);
            }
        }

        [Theory(DisplayName = "Headers Must Be Valid")]
        [InlineData("content-length")]
        [InlineData(":126")]
        [InlineData(" :126")]
        [InlineData("content:gitbuff:content")]
        public async void StompHeadersFail(string header)
        {
            var command = StompParser.Command.MESSAGE;
            var eol = "\r\n";

            await Assert.ThrowsAsync<HeaderParseException>(async () =>
            {
                using (var memStream = new MemoryStream())
                using (var streamWriter = new StreamWriter(memStream))
                {
                    streamWriter.Write(command);
                    streamWriter.Write(eol);
                    streamWriter.Write(header);
                    streamWriter.Write(eol);
                    streamWriter.Write(eol);
                    streamWriter.Write(0x00);
                    streamWriter.Flush();

                    memStream.Position = 0;

                    var parsedCommand = await StompParser.ReadStompCommand(memStream);
                    var parsedHeaders = await StompParser.ReadStompHeaders(parsedCommand.Item1, parsedCommand.Item2);
                }
            });
        }
    }
}