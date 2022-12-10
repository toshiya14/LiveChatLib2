using LiveChatLib2.Configs;
using LiveChatLib2.Models.QueueMessages;
using LiveChatLib2.Models.QueueMessages.WorkItems;
using LiveChatLib2.Queue;
using LiveChatLib2.Storage;
using NLog;

namespace LiveChatLib2.Parsers;

internal partial class BilibiliParser : IBilibiliParser
{
    protected static ILogger log = LogManager.GetCurrentClassLogger();

    protected BilibiliParserConfig Config { get; set; }
    public IBilibiliUserInfoStorage UserStorage { get; }
    protected IMessageQueue<RecordWorkItem> RecordQueue { get; set; }
    protected IMessageQueue<CrawlerWorkItem> CrawlerQueue { get; set; }
    protected IMessageQueue<SendWorkItem> SendQueue { get; set; }
    protected ParserListeningStatus State { get; private set; }

    public BilibiliParser(
        IBilibiliUserInfoStorage userStorage,
        IMessageQueue<RecordWorkItem> recordQueue,
        IMessageQueue<CrawlerWorkItem> crawlerQueue,
        IMessageQueue<SendWorkItem> sendQueue,
        BilibiliParserConfig config)
    {
        log.Trace("BilibiliParser would be created.");
        this.UserStorage = userStorage;
        this.RecordQueue = recordQueue;
        this.CrawlerQueue = crawlerQueue;
        this.SendQueue = sendQueue;
        this.Config = config;
        this.State = ParserListeningStatus.Disconnected;
        this.lastSendHeartBeatTime = default;
        this.Proxy = new WebSocketSharp.Async.WebSocketClient(WEBSOCKET_URL);
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        log.Info("BilibiliParser Started.");
        await this.StartProxy(cancellationToken);
    }

    public void Dispose()
    {
        log.Trace("BilibiliParser disposed.");
        this.Proxy?.Dispose();
    }
}
