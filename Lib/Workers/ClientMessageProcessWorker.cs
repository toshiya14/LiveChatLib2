using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveChatLib2.Models;
using LiveChatLib2.Models.QueueMessages;
using LiveChatLib2.Models.QueueMessages.WorkItems;
using LiveChatLib2.Queue;
using LiveChatLib2.Services;
using LiveChatLib2.Storage;
using LiveChatLib2.Utils;
using NLog;
using WebSocketSharp.Async;

namespace LiveChatLib2.Workers;
internal class ClientMessageProcessWorker : IWorker<ClientMessage>
{
    public ClientMessageProcessWorker(
        WebSocketServer server,
        IMessageQueue<WorkItemBase> parserQueue,
        IBilibiliUserInfoStorage bilibiliUserInfoStorage
    )
    {
        this.Server = server;
        this.ParserQueue = parserQueue;
        this.BilibiliUserInfoStorage = bilibiliUserInfoStorage;
    }

    private WebSocketServer Server { get; }
    private IMessageQueue<WorkItemBase> ParserQueue { get; }
    public IBilibiliUserInfoStorage BilibiliUserInfoStorage { get; }

    private readonly ILogger log = LogManager.GetCurrentClassLogger();

    public async Task DoWork(ClientMessage message, CancellationToken cancellationToken)
    {
        switch (message.Processor)
        {
            case "bilibili":
                await Task.Run(
                    () => this.ProcessBilibiliMessages(message)
                    , cancellationToken
                );
                break;

            default:
                log.Warn($"Got unknown processor `{message.Processor}` client message.");
                break;
        }
    }

    private void ProcessBilibiliMessages(ClientMessage message)
    {
        ArgumentNullException.ThrowIfNull(message.Parameters);

        switch (message.Action)
        {
            case "queryUserInfo":
                if (!message.Parameters.ContainsKey("id"))
                    throw new ArgumentException("userId is not specified in parameters.");

                var userId = message.Parameters["id"].ToString()!;
                ArgumentNullException.ThrowIfNull(userId, "id");
                ArgumentNullException.ThrowIfNull(message.ClientInfo, "clientInfo");
                this.BilibiliQueryUser(
                    message.ClientInfo with { Action = ClientAction.Send }, 
                    userId
                );
                break;
        }
    }

    private void BilibiliQueryUser(ClientInfo client, string userId)
    {
        const string SOURCE = "bilibili";
        // Try to pick user first.
        var user = this.BilibiliUserInfoStorage.PickUserInformation(userId);
        if (user != null)
        {
            this.ParserQueue.Enqueue(
                new SendWorkItem(
                    SOURCE,
                    client,
                    ObjectSerializer.ToJsonBinary(
                        new ClientMessageResponse(
                            "user-info",
                            user
                        )
                    )
                )
            );
        }
        else
        {
            this.ParserQueue.Enqueue(
                new BilibiliUserCrawlerWorkItem(
                    userId,
                    client
                )
            );
        }
    }
}
