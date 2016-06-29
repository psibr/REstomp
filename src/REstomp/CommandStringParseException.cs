public class CommandStringParseException : System.Exception
{
    public CommandStringParseException() { }
    public CommandStringParseException( string message ) : base( message ) { }
    public CommandStringParseException( string message, System.Exception inner ) : base( message, inner ) { }
}