namespace REstomp
{
    public class ContentLengthException : System.Exception
    {
        public ContentLengthException() { }
        public ContentLengthException( string message ) : base( message ) { }
        public ContentLengthException( string message, System.Exception inner ) : base( message, inner ) { }
    }
}