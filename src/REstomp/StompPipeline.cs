using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace REstomp
{

    using AppFunc = Func<IDictionary<string, object>, Task>;
    using MidFunc = Func<Func<IDictionary<string, object>, Task>, Func<IDictionary<string, object>, Task>>;

    public sealed class StompPipeline
    {
        private AppFunc Application;

        public StompPipeline(Stack<MidFunc> builder)
        {
            AppFunc current = (IDictionary<string, object> environment) => Task.CompletedTask;
            while (builder.Count > 0)
            {
                current = builder.Pop().Invoke(current);
            }

            Application = current;
        }

        public async Task<StompFrame> Process(IDictionary<string, object> environment)
        {
            await Application.Invoke(environment);

            StompFrame responseFrame = null;

            var command = environment["stomp.reponseMethod"] as string;

            if (!string.IsNullOrWhiteSpace(command) && StompParser.Command.IsSupported(command))
            {
                var headers = (ImmutableArray<KeyValuePair<string, string>>)environment["stomp.responseHeaders"];
                var body = (ImmutableArray<byte>)environment["stomp.responseBody"];

                responseFrame = new StompFrame(command, headers, body);
            }

            return responseFrame;
        }
    }
}