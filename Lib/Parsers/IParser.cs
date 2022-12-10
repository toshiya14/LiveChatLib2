namespace LiveChatLib2.Parsers;

public interface IParser
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

