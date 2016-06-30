using System.Collections.Immutable;

namespace REstomp
{
    public class StompFrame
    {
        public StompFrame(string commandString, ImmutableDictionary<string, string> headers, byte[] body)
        {
            CommandString = commandString;
            Headers = headers;
            Body = body;
        }

        public StompFrame(string commandString, ImmutableDictionary<string, string> headers)
            : this(commandString, headers, new byte[0])
        {
        }

        public StompFrame(string commandString)
            : this(commandString, ImmutableDictionary<string, string>.Empty, new byte[0])
        {
        }


        public string CommandString { get; }

        public ImmutableDictionary<string, string> Headers { get; }

        public byte[] Body { get; }
    }
}