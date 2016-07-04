using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace REstomp
{
    public class StompFrame
    {
        public StompFrame(string command, IDictionary<string, string> headers, byte[] body)
        {
            Command = command;
            Headers = headers?.ToImmutableDictionary() ?? ImmutableDictionary<string, string>.Empty;

            if(body != null)
                Body = body.ToImmutableArray();
        }

        public StompFrame(string command, IDictionary<string, string> headers, ImmutableArray<byte> body)
        {
            Command = command;
            Headers = headers?.ToImmutableDictionary() ?? ImmutableDictionary<string, string>.Empty;
            Body = body;
        }

        public StompFrame(string command, IDictionary<string, string> headers)
            : this(command, headers, null)
        {
        }

        public StompFrame(string command)
            : this(command, ImmutableDictionary<string, string>.Empty, null)
        {
        }

        private StompFrame()
        { }


        public string Command { get; }

        public ImmutableDictionary<string, string> Headers { get; }

        public ImmutableArray<byte> Body { get; }

        public static StompFrame Empty { get; } = new StompFrame();


        public StompFrame With<TMember>(Expression<Func<StompFrame, TMember>> mutationSelectorExpression, TMember value)
        {
            var memberName = ((MemberExpression)mutationSelectorExpression.Body).Member.Name;
            var command = Command;
            var headers = Headers;
            var body = Body;

            var obj = (object)value;

            switch (memberName)
            {
                case nameof(Command):
                    command = (string)obj;
                    break;
                case nameof(Headers):
                    headers = (ImmutableDictionary<string, string>)obj;
                    break;
                case nameof(Body):
                    body = (ImmutableArray<byte>)obj;
                    break;
            }

            return new StompFrame(command, headers, body);
        }

    }
}