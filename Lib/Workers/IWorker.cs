namespace LiveChatLib2.Workers;

internal interface IWorker<T>
{
    Type ConsumerType { get; }
    Task DoWork(T message, CancellationToken cancellationToken);
}
