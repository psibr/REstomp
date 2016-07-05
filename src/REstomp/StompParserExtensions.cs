using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace REstomp
{
    public static class StompParserExtensions
    {
        internal static async Task<Tuple<PrependableStream<TStream>, StompFrame, CancellationToken>> ReadStompCommand<TStream>(
            this PrependableStream<TStream> stream, CancellationToken cancellationToken)
            where TStream : Stream
        {
            var result = await StompParser.ReadStompCommand(stream, cancellationToken);
            return Tuple.Create(result.Item1, result.Item2, cancellationToken);
        }

        internal static async Task<Tuple<PrependableStream<TStream>, StompFrame, CancellationToken>> ThenReadStompHeaders<TStream>(
            this Task<Tuple<PrependableStream<TStream>, StompFrame, CancellationToken>> commandResultTask)
            where TStream : Stream
        {
            var commandResult = await commandResultTask;
            var result = await StompParser.ReadStompHeaders(commandResult.Item1, commandResult.Item2, commandResult.Item3);
            return Tuple.Create(result.Item1, result.Item2, commandResult.Item3);
        }

        internal static async Task<Tuple<PrependableStream<TStream>, StompFrame>> ThenReadStompBody<TStream>(
            this Task<Tuple<PrependableStream<TStream>, StompFrame, CancellationToken>> headerResultTask)
            where TStream : Stream
        {
            var headerResult = await headerResultTask;
            var result = await StompParser.ReadStompBody(headerResult.Item1, headerResult.Item2, headerResult.Item3);
            return Tuple.Create(result.Item1, result.Item2);
        }

        public static StompFrame UnWrapFrame<TStream>(this Tuple<TStream, StompFrame> result)
            where TStream : Stream
        {
            return result.Item2;
        }

        public static async Task<StompFrame> UnWrapFrame<TStream>(this Task<Tuple<TStream, StompFrame>> result)
            where TStream : Stream
        {
            return (await result).UnWrapFrame();
        }
    }
}