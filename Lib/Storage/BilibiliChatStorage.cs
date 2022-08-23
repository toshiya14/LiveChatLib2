using LiteDB;
using LiveChatLib2.Models.MessageRecords;
using LiveChatLib2.Models.QueueMessages;
using LiveChatLib2.Models.QueueMessages.WorkItems;
using LiveChatLib2.Models.RemotePackages;
using LiveChatLib2.Queue;

namespace LiveChatLib2.Storage;

internal class BilibiliChatStorage : StorageBase, IBilibiliChatStorage
{
    private BilibiliUserInfoStorage UserStorage { get; }
    private IMessageQueue<WorkItemBase> Queue { get; }

    public BilibiliChatStorage(
        BilibiliUserInfoStorage userStorage,
        IMessageQueue<WorkItemBase> queue
    )
    {
        this.UserStorage = userStorage;
        this.Queue = queue;
    }

    public async Task RecordChatMessage(BilibiliMessageRecord message, CancellationToken cancellationToken)
    {
        this.InitializeDatabaseFolder();
        await Task.Run(() =>
        {
            using var db = new LiteDatabase(ChatLogDatabasePath);
            var chats = db.GetCollection<BilibiliMessageRecord>();
            chats.Insert(message);
            chats.EnsureIndex(x => x.SenderName);
            chats.EnsureIndex(x => x.ReceiveTime);
            chats.EnsureIndex(x => x.SenderId);
        }, cancellationToken);
    }

    public async Task RecordRemotePackage(BilibiliRemotePackage package, CancellationToken cancellationToken)
    {
        this.InitializeDatabaseFolder();
        await Task.Run(() =>
        {
            using var db = new LiteDatabase(SampleDatabasePath);
            var cols = db.GetCollection<BilibiliRemotePackage>();
            cols.Insert(package);
        }, cancellationToken);
    }

    public async Task<IList<BilibiliMessageRecord>> PickLastestComments(int count, CancellationToken cancellationToken)
    {
        base.InitializeDatabaseFolder();

        return await Task.Run(() =>
        {
            using var db = new LiteDatabase(base.ChatLogDatabasePath);
            var chats = db.GetCollection<BilibiliMessageRecord>().Include(x=>x.Type == "comment" || x.Type == "gift").Find(Query.All("ReceiveTime", Query.Descending)).Take(count);

            foreach (var i in chats)
            {
                if (i.SenderId != null)
                {
                    var user = this.UserStorage.PickUserInformation(i.SenderId);
                    if (user != null)
                    {
                        if (user.Face != null)
                        {
                            i.Avatar = user.Face;
                        }
                        else
                        {
                            this.Queue.Enqueue(
                                new BilibiliUserCrawlerWorkItem(i.SenderId)
                            );
                        }
                    }
                }
            }

            return chats.Reverse().ToList();
        }, cancellationToken);
    }
}
