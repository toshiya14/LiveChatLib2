namespace LiveChatLib2.Exceptions;

internal class DataFormatException : Exception
{
    public DataFormatException() : base() { }
    public DataFormatException(string? message, Exception? inner = null) : base(message, inner) { }
}
