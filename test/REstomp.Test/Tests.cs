using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        public static IEnumerable<object[]> EncodingCharsetEnumerable =>
            new List<object[]>
            {
                new object[] { "utf-8", Encoding.UTF8 },
                new object[] { "utf-7", Encoding.UTF7 },
                new object[] { "utf-16", Encoding.Unicode },
                new object[] { "utf-32", Encoding.UTF32 },
                new object[] { "ascii", Encoding.ASCII }
            };

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
                streamWriter.Write((char)0x00);
                streamWriter.Flush();

                memStream.Position = 0;

                var parsedCommand = await StompParser.ReadStompCommand(memStream.AsPrependableStream());

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
                    streamWriter.Write((char)0x00);
                    streamWriter.Flush();

                    memStream.Position = 0;

                    await StompParser.ReadStompCommand(memStream.AsPrependableStream());
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

            Assert.True(newFrame.Headers.IsDefault);
            Assert.True(newFrame.Body.IsDefault);
        }

        [Theory(DisplayName = "StompFrame With Headers")]
        [InlineData("content-length", "126")]
        public void StompFrameWithHeaders(string headerKey, string headerValue)
        {
            var header = new KeyValuePair<string, string>(headerKey, headerValue);
            var headerArray = new KeyValuePair<string, string>[1];
            headerArray[0] = header;
            var expectation = new StompFrame(StompParser.Command.MESSAGE, headerArray);

            var newFrame = new StompFrame(StompParser.Command.MESSAGE)
                .With(frame => frame.Headers, headerArray);
            Assert.NotNull(newFrame);
            Assert.Equal(expectation.Command, newFrame.Command);

            Assert.Equal(expectation.Headers[0].Key, newFrame.Headers[0].Key);
            Assert.Equal(expectation.Headers[0].Value, newFrame.Headers[0].Value);
            Assert.True(newFrame.Body.IsDefault);
        }

        [Fact(DisplayName = "Headers Parse")]
        public async void StompParseHeaders()
        {
            var command = StompParser.Command.MESSAGE;
            var header = new KeyValuePair<string, string>("content-length", "126");
            var headerArray = new KeyValuePair<string, string>[1];
            headerArray[0] = header;

            var expectation = new StompFrame(command, headerArray);

            using (var memStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memStream))
            {
                var eol = "\r\n";
                streamWriter.Write(command);
                streamWriter.Write(eol);
                foreach (var headerPair in headerArray)
                {
                    streamWriter.Write($"{headerPair.Key}:{headerPair.Value}");
                }
                streamWriter.Write(eol);
                streamWriter.Write(eol);
                streamWriter.Write((char)0x00);
                streamWriter.Flush();

                memStream.Position = 0;

                var parsedCommand = await StompParser.ReadStompCommand(memStream.AsPrependableStream());
                var parsedHeaders = await StompParser.ReadStompHeaders(parsedCommand.Item1, parsedCommand.Item2);

                Assert.StrictEqual(expectation.Command, parsedCommand.Item2.Command);
                Assert.Equal(expectation.Headers[0], parsedHeaders.Item2.Headers[0]);
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
                    streamWriter.Write((char)0x00);
                    streamWriter.Flush();

                    memStream.Position = 0;

                    var parsedCommand = await StompParser.ReadStompCommand(memStream.AsPrependableStream());
                    var parsedHeaders = await StompParser.ReadStompHeaders(parsedCommand.Item1, parsedCommand.Item2);
                }
            });
        }

        [Theory(DisplayName = "Content Length Must Have Int Value")]
        [InlineData("content-length:ten")]
        [InlineData("content-length:3000000000")]
        public async void ContentLengthParse(string header)
        {
            var command = StompParser.Command.MESSAGE;
            var eol = "\r\n";

            await Assert.ThrowsAsync<ContentLengthException>(async () =>
            {
                using (var memStream = new MemoryStream())
                using (var streamWriter = new StreamWriter(memStream))
                {
                    streamWriter.Write(command);
                    streamWriter.Write(eol);
                    streamWriter.Write(header);
                    streamWriter.Write(eol);
                    streamWriter.Write(eol);
                    streamWriter.Write("0123456789");
                    streamWriter.Write((char)0x00);
                    streamWriter.Flush();

                    memStream.Position = 0;

                    var parsedCommand = await StompParser.ReadStompCommand(memStream.AsPrependableStream());
                    var parsedHeaders = await StompParser.ReadStompHeaders(parsedCommand.Item1, parsedCommand.Item2);
                    var parsedBody = await StompParser
                        .ReadStompBody(parsedHeaders.Item1, parsedHeaders.Item2, CancellationToken.None);
                }
            });
        }

        [Fact(DisplayName = "Content Cannot Be Longer Than Content Length Value")]
        public async void ContentTooLong()
        {
            var command = StompParser.Command.MESSAGE;
            var eol = "\n";
            var header = "content-length:5";

            await Assert.ThrowsAsync<ContentLengthException>(async () =>
            {
                using (var memStream = new MemoryStream())
                using (var streamWriter = new StreamWriter(memStream))
                {
                    streamWriter.Write(command);
                    streamWriter.Write(eol);
                    streamWriter.Write(header);
                    streamWriter.Write(eol);
                    streamWriter.Write(eol);
                    streamWriter.Write("0123456789");
                    streamWriter.Write((char)0x00);
                    streamWriter.Flush();

                    memStream.Position = 0;

                    var parsedCommand = await StompParser.ReadStompCommand(memStream.AsPrependableStream());
                    var parsedHeaders = await StompParser.ReadStompHeaders(parsedCommand.Item1, parsedCommand.Item2);
                    var parsedBody = await StompParser
                        .ReadStompBody(parsedHeaders.Item1, parsedHeaders.Item2, CancellationToken.None);
                }
            });
        }

        [Fact(DisplayName = "Content Length Matches Content Length Value")]
        public async void ContentLengthMatchesValue()
        {
            var bodyString = "0123456789abcdefghijk1234567890abcd";
            var command = StompParser.Command.MESSAGE;

            var header = new KeyValuePair<string, string>
                ("content-length", bodyString.Length.ToString());
            var headerArray = new KeyValuePair<string, string>[1];
            headerArray[0] = header;
            var body = Encoding.UTF8.GetBytes(bodyString);

            var expectation = new StompFrame(command, headerArray, body);

            using (var memStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memStream))
            {
                var eol = "\r\n";
                streamWriter.Write(command);
                streamWriter.Write(eol);
                foreach (var headerPair in headerArray)
                {
                    streamWriter.Write($"{headerPair.Key}:{headerPair.Value}");
                }
                streamWriter.Write(eol);
                streamWriter.Write(eol);
                streamWriter.Write(bodyString);
                streamWriter.Write((char)0x00);
                streamWriter.Flush();

                memStream.Position = 0;

                var parsedCommand = await StompParser.ReadStompCommand(memStream.AsPrependableStream());
                var parsedHeaders = await StompParser.ReadStompHeaders(parsedCommand.Item1, parsedCommand.Item2);
                var parsedBody = await StompParser
                    .ReadStompBody(parsedHeaders.Item1, parsedHeaders.Item2, CancellationToken.None);

                Assert.StrictEqual(expectation.Command, parsedCommand.Item2.Command);

                Assert.Equal(expectation.Headers[0], parsedHeaders.Item2.Headers[0]);
                Assert.True(Encoding.UTF8.GetString(expectation.Body.ToArray()) 

                    == Encoding.UTF8.GetString(parsedBody.Item2.Body.ToArray()));
                Assert.Equal(parsedBody.Item2.Body.Length, bodyString.Length);
            }
        }

        [Fact(DisplayName = "Null Byte Signals End of Non-Content Length Frame")]
        public async void ContentEndsOnNull()
        {
            var bodyString = "0123456789abcdefghijk1234567890abcd";
            var command = StompParser.Command.MESSAGE;
            var header = new KeyValuePair<string, string>("key", "value");
            var headerArray = new KeyValuePair<string, string>[1];
            headerArray[0] = header;
            var body = Encoding.UTF8.GetBytes(bodyString);

            var expectation = new StompFrame(command, headerArray, body);

            using (var memStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memStream))
            {
                var eol = "\r\n";
                streamWriter.Write(command);
                streamWriter.Write(eol);
                foreach (var headerPair in headerArray)
                {
                    streamWriter.Write($"{headerPair.Key}:{headerPair.Value}");
                }
                streamWriter.Write(eol);
                streamWriter.Write(eol);
                streamWriter.Write(bodyString);
                streamWriter.Write((char)0x00);
                streamWriter.Flush();

                memStream.Position = 0;

                var parsedCommand = await StompParser.ReadStompCommand(memStream.AsPrependableStream());
                var parsedHeaders = await StompParser.ReadStompHeaders(parsedCommand.Item1, parsedCommand.Item2);
                var parsedBody = await StompParser
                    .ReadStompBody(parsedHeaders.Item1, parsedHeaders.Item2, CancellationToken.None);

                Assert.StrictEqual(expectation.Command, parsedCommand.Item2.Command);
                Assert.Equal(expectation.Headers[0], parsedHeaders.Item2.Headers[0]);
                Assert.True(Encoding.UTF8.GetString(expectation.Body.ToArray())
                            == Encoding.UTF8.GetString(parsedBody.Item2.Body.ToArray()));
                Assert.Equal(parsedBody.Item2.Body.Length, bodyString.Length);
            }
        }

        [Fact(DisplayName = "Frames Parse")]
        public async void FrameParses()
        {
            var bodyString = "0123456789abcdefghijk1234567890abcd";
            var command = StompParser.Command.MESSAGE;
            var header = new KeyValuePair<string, string>("key", "value");
            var headerArray = new KeyValuePair<string, string>[1];
            headerArray[0] = header;
            var body = Encoding.UTF8.GetBytes(bodyString);

            var expectation = new StompFrame(command, headerArray, body);

            using (var memStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memStream))
            {
                var eol = "\r\n";
                streamWriter.Write(command);
                streamWriter.Write(eol);
                foreach (var headerPair in headerArray)
                {
                    streamWriter.Write($"{headerPair.Key}:{headerPair.Value}");
                }
                streamWriter.Write(eol);
                streamWriter.Write(eol);
                streamWriter.Write(bodyString);
                streamWriter.Write((char)0x00);
                streamWriter.Flush();

                memStream.Position = 0;

                var parser = new StompParser();
                var frame = await parser.ReadStompFrame(memStream).UnWrapFrame();

                Assert.StrictEqual(expectation.Command, frame.Command);
                Assert.Equal(expectation.Headers[0], frame.Headers[0]);
                Assert.True(Encoding.UTF8.GetString(expectation.Body.ToArray())
                    == Encoding.UTF8.GetString(frame.Body.ToArray()));
                Assert.Equal(frame.Body.Length, bodyString.Length);

            }
        }

        [Fact(DisplayName = "GetContentLengthHeader returns empty if no content length header")]
        public void EmptyIfNoLengthHeader()
        {
            var bodyString = "0123456789abcdefghijk1234567890abcd";
            var command = StompParser.Command.MESSAGE;
            var header = new KeyValuePair<string, string>("key", "value");
            var headerArray = new KeyValuePair<string, string>[1];
            headerArray[0] = header;
            var body = Encoding.UTF8.GetBytes(bodyString);

            var frame = new StompFrame(command, headerArray, body);

            Assert.Equal(ContentLengthHeader.Empty, frame.GetContentLengthHeader());
        }

        [Fact(DisplayName = "GetContentLengthHeader handles valid int strings")]
        public void HandleValidIntStrings()
        {
            var bodyString = "0123456789abcdefghijk1234567890abcd";
            var command = StompParser.Command.MESSAGE;
            var header = new KeyValuePair<string, string>("content-length", bodyString.Length.ToString());
            var headerArray = new KeyValuePair<string, string>[1];
            headerArray[0] = header;
            var body = Encoding.UTF8.GetBytes(bodyString);

            var frame = new StompFrame(command, headerArray, body);

            Assert.Equal(bodyString.Length, frame.GetContentLengthHeader().ContentLength);
        }

        [Fact(DisplayName = "GetContentLengthHeader throws if invalid int string")]
        public void ThrowExceptionIfInvalid()
        {
            var bodyString = "0123456789abcdefghijk1234567890abcd";
            var command = StompParser.Command.MESSAGE;
            var header = new KeyValuePair<string, string>("content-length", "thirty-five");
            var headerArray = new KeyValuePair<string, string>[1];
            headerArray[0] = header;
            var body = Encoding.UTF8.GetBytes(bodyString);

            var frame = new StompFrame(command, headerArray, body);

            Assert.Throws<ContentLengthException>(() =>
            {
                frame.GetContentLengthHeader();
            });
        }

        [Fact(DisplayName = "GetContentTypeHeader returns empty if no header")]
        public void EmptyIfNoTypeHeader()
        {
            var bodyString = "0123456789abcdefghijk1234567890abcd";
            var command = StompParser.Command.MESSAGE;
            var header = new KeyValuePair<string, string>("key", "value");
            var headerArray = new KeyValuePair<string, string>[1];
            headerArray[0] = header;
            var body = Encoding.UTF8.GetBytes(bodyString);

            var frame = new StompFrame(command, headerArray, body);

            Assert.Equal(ContentTypeHeader.Empty, frame.GetContentTypeHeader());
        }

        [Fact(DisplayName = "GetContentTypeHeader captures content-type and charset")]
        public void ValidTypeHeader()
        {
            var bodyString = "0123456789abcdefghijk1234567890abcd";
            var command = StompParser.Command.MESSAGE;
            var header = new KeyValuePair<string, string>("content-type", "application/json;charset=utf-32");
            var headerArray = new KeyValuePair<string, string>[1];
            headerArray[0] = header;
            var body = Encoding.UTF8.GetBytes(bodyString);

            var frame = new StompFrame(command, headerArray, body);

            var resultHeader = frame.GetContentTypeHeader();

            Assert.Equal("application/json", resultHeader.ContentType);
            Assert.Equal("utf-32", resultHeader.Charset);
        }

        [Theory(DisplayName = "ContentTypeHeader.GetEncoding returns correct encodings")]
        [MemberData(nameof(EncodingCharsetEnumerable))]
        
        public void SupportEncoding(string charset, Encoding encoding)
        {
            var bodyString = "0123456789abcdefghijk1234567890abcd";
            var command = StompParser.Command.MESSAGE;
            var header = new KeyValuePair<string, string>("content-type", $"application/json;charset={charset}");
            var headerArray = new KeyValuePair<string, string>[1];
            headerArray[0] = header;
            var body = Encoding.UTF8.GetBytes(bodyString);

            var frame = new StompFrame(command, headerArray, body);

            Assert.Equal(encoding, frame.GetContentTypeHeader().GetEncoding());
        }

        [Theory(DisplayName = "ContentTypeHeader.GetEncoding returns utf-8 if no charset provided")]
        [InlineData("application/json")]
        [InlineData("application/json;charset=")]
        [InlineData("application/json;")]
        [InlineData("application/json;;")]

        public void NoCharsetProvided(string contentTypeValue)
        {
            var bodyString = "0123456789abcdefghijk1234567890abcd";
            var command = StompParser.Command.MESSAGE;
            var header = new KeyValuePair<string, string>("content-type", contentTypeValue);
            var headerArray = new KeyValuePair<string, string>[1];
            headerArray[0] = header;
            var body = Encoding.UTF8.GetBytes(bodyString);

            var frame = new StompFrame(command, headerArray, body);

            var resultHeader = frame.GetContentTypeHeader();

            Assert.Equal("application/json", resultHeader.ContentType);
            Assert.Equal(Encoding.UTF8, frame.GetContentTypeHeader().GetEncoding());
        }

        [Theory(DisplayName = "StompFrame.ToString returns appropriate string")]
        [MemberData(nameof(EncodingCharsetEnumerable))]
        public void FrameToString(string charset, Encoding encoding)
        {
            var bodyString = "0123456789abcdefghijk1234567890abcd";
            var command = StompParser.Command.MESSAGE;
            var header = new KeyValuePair<string, string>("content-type", $"application/json;charset={charset}");
            var headerArray = new KeyValuePair<string, string>[1];
            headerArray[0] = header;
            var body = Encoding.UTF8.GetBytes(bodyString);

            var frame = new StompFrame(command, headerArray, body);

            var expectation =
                "MESSAGE\n" +
                $"content-type:application/json;charset={charset}\n" +
                "\n" +
                encoding.GetString(body) +
                "\0";

            Assert.Equal(expectation, frame.ToString());
        }

    }
}