namespace LiveChatLib2.Workers;

internal interface IWorker<T>
{
    Task DoWork(T message, CancellationToken cancellationToken);
}
