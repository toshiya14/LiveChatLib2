using System.Collections.Concurrent;

namespace LiveChatLib2.Queue;

internal class EasyMessageQueue<T> : ConcurrentQueue<T>, IMessageQueue<T> where T : class
{
    public Type? ConsumerType { get; set; }

    public new void Enqueue(T obj)
    {
        base.Enqueue(obj);
    }

    public T? Dequeue()
    {
        if (this.TryDequeue(out var item))
            return item;
        return null;
    }

}
