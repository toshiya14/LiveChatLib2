using LiteDB;
using LiveChatLib2.Models.MessageRecords;
using LiveChatLib2.Models.QueueMessages;
using LiveChatLib2.Models.QueueMessages.WorkItems;
using LiveChatLib2.Models.RemotePackages;
using LiveChatLib2.Queue;
using NLog;

namespace LiveChatLib2.Storage;

internal class BilibiliChatStorage : StorageBase, IBilibiliChatStorage
{
    private IBilibiliUserInfoStorage UserStorage { get; }
    private IMessageQueue<CrawlerWorkItem> Queue { get; }

    private static readonly ILogger log = LogManager.GetCurrentClassLogger();

    public BilibiliChatStorage(
        IBilibiliUserInfoStorage userStorage,
        IMessageQueue<CrawlerWorkItem> queue
    )
    {
        log.Trace("BilibiliChatStorage Initialized.");
        this.UserStorage = userStorage;
        this.Queue = queue;
        this.InitializeDatabaseFolder();
    }

    public async Task RecordChatMessage(BilibiliMessageRecord message, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            lock (this.ChatLogDatabasePath)
            {
                using var db = new LiteDatabase(this.ChatLogDatabasePath);
                var chats = db.GetCollection<BilibiliMessageRecord>();
                chats.Insert(message);
                chats.EnsureIndex(x => x.SenderName);
                chats.EnsureIndex(x => x.ReceiveTime);
                chats.EnsureIndex(x => x.SenderId);
            }
        }, cancellationToken);
    }

    public async Task RecordRemotePackage(BilibiliRemotePackage package, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            lock (this.SampleDatabasePath)
            {
                using var db = new LiteDatabase(this.SampleDatabasePath);
                var cols = db.GetCollection<BilibiliRemotePackage>();
                cols.Insert(package);
            }
        }, cancellationToken);
    }

    public async Task<IList<BilibiliMessageRecord>> PickLastestComments(int count, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            IEnumerable<BilibiliMessageRecord> chats;
            lock (this.ChatLogDatabasePath)
            {
                using var db = new LiteDatabase(this.ChatLogDatabasePath);
                chats = db.GetCollection<BilibiliMessageRecord>().Include(x=>x.Type == "comment" || x.Type == "gift").Find(Query.All("ReceiveTime", Query.Descending)).Take(count);
            }

            foreach (var i in chats!)
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
                            log.Trace($"Collect user information with uid: {i.SenderId}");
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
