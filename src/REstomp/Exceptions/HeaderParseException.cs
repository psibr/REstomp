namespace REstomp
{
    public class HeaderParseException : System.Exception
    {
        public HeaderParseException() { }
        public HeaderParseException( string message ) : base( message ) { }
        public HeaderParseException( string message, System.Exception inner ) : base( message, inner ) { }
    }
}