namespace LiveChatLib2.Queue;

internal interface IMessageQueue<T>
{
    Type? ConsumerType { get; set; }
    void Enqueue(T obj);
    T? Dequeue();
}
