namespace REstomp
{
    public class CommandParseException : System.Exception
    {
        public CommandParseException() { }
        public CommandParseException( string message ) : base( message ) { }
        public CommandParseException( string message, System.Exception inner ) : base( message, inner ) { }
    }
}