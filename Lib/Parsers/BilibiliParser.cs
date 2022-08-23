using LiveChatLib2.Configs;
using LiveChatLib2.Models.QueueMessages;
using LiveChatLib2.Queue;
using LiveChatLib2.Storage;
using NLog;

namespace LiveChatLib2.Parsers;

internal partial class BilibiliParser : IBilibiliParser
{
    protected DateTime lastSendHeartBeatTime;
    protected DateTime lastReceiveTime;
    protected ILogger log = LogManager.GetCurrentClassLogger();

    protected BilibiliParserConfig Config { get; set; }
    public BilibiliUserInfoStorage UserStorage { get; }
    protected IMessageQueue<WorkItemBase> Queue { get; set; }
    protected ParserListeningStatus State { get; private set; }

    public BilibiliParser(
        BilibiliUserInfoStorage userStorage,
        IMessageQueue<WorkItemBase> queue,
        BilibiliParserConfig config)
    {
        this.UserStorage = userStorage;
        this.Queue = queue;
        this.Config = config;
        log.Info("BilibiliParser created.");
        this.State = ParserListeningStatus.Disconnected;
        this.lastSendHeartBeatTime = default;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        log.Info("BilibiliParser Started.");
        await this.StartProxy(cancellationToken);
    }

    public void Dispose()
    {
        this.Proxy?.Dispose();
    }
}
