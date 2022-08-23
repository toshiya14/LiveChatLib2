namespace LiveChatLib2.Parsers;

public interface IParser : IDisposable
{
    Task Start(CancellationToken cancellationToken);
}

public enum ParserListeningStatus
{
    Disconnected,
    Connecting,
    Connected,
    BadCommunication,
}

