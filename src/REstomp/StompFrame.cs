using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace REstomp
{
    public class StompFrame
    {
        public StompFrame(string command, KeyValuePair<string, string>[] headers, byte[] body)
        {
            Command = command;
            Headers = headers?.ToImmutableArray() ?? ImmutableArray<KeyValuePair<string, string>>.Empty;

            if (body != null)
                Body = body.ToImmutableArray();
        }

        public StompFrame(string command, ImmutableArray<KeyValuePair<string, string>> headers, byte[] body)
        {
            Command = command;
            Headers = headers;

            if (body != null)
                Body = body.ToImmutableArray();
        }

        public StompFrame(string command, KeyValuePair<string, string>[] headers, ImmutableArray<byte> body)
        {
            Command = command;
            Headers = headers?.ToImmutableArray() ?? ImmutableArray<KeyValuePair<string, string>>.Empty;
            Body = body;
        }

        public StompFrame(string command, ImmutableArray<KeyValuePair<string, string>> headers, ImmutableArray<byte> body)
        {
            Command = command;
            Headers = headers;
            Body = body;
        }

        public StompFrame(string command, KeyValuePair<string, string>[] headers)
            : this(command, headers, null)
        {
        }

        public StompFrame(string command, ImmutableArray<KeyValuePair<string, string>> headers)
            : this(command, headers, null)
        {
        }

        public StompFrame(string command)
            : this(command, ImmutableArray<KeyValuePair<string, string>>.Empty, null)
        {
        }

        private StompFrame()
        { }


        public string Command { get; }

        public ImmutableArray<KeyValuePair<string, string>> Headers { get; }

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
                    headers = (ImmutableArray<KeyValuePair<string, string>>)obj;
                    break;
                case nameof(Body):
                    body = (ImmutableArray<byte>)obj;
                    break;
            }

            return new StompFrame(command, headers, body);
        }

        public StompFrame With(
            Expression<Func<StompFrame, ImmutableArray<KeyValuePair<string, string>>>> mutationSelectorExpression,
            KeyValuePair<string, string>[] value)
        {
            return With(mutationSelectorExpression, value.ToImmutableArray());
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            var encoding = this.GetContentTypeHeader().GetEncoding();

            var builder = new StringBuilder();

            //Command
            builder.Append(Command);
            builder.Append("\n");

            //Headers
            foreach (var keyValuePair in Headers)
            {
                builder.Append($"{keyValuePair.Key}:{keyValuePair.Value}\n");
            }
            //Body
            builder.Append("\n");
            builder.Append(encoding.GetString(Body.ToArray()));
            builder.Append((char)0x00);

            return builder.ToString();
        }
    }

}