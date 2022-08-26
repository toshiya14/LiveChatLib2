using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveChatLib2.Models.QueueMessages;
using LiveChatLib2.Models.QueueMessages.WorkItems;
using WebSocketSharp.Async;

namespace LiveChatLib2.Workers;
internal class SendWorker : IWorker<SendWorkItem>
{
    public SendWorker(WebSocketServer server)
    {
        this.Server = server;
    }

    private WebSocketServer Server { get; }

    public async Task DoWork(SendWorkItem parameters, CancellationToken cancellationToken)
    {
        _ = this.SendMessage(parameters, cancellationToken);
        await Task.Delay(50, cancellationToken);
    }

    public async Task SendMessage(SendWorkItem parameters, CancellationToken cancellationToken)
    {
        var target = parameters.Target;

        switch (target.Action) {
            case Models.ClientAction.Send:
                ArgumentNullException.ThrowIfNull(target.ClientId);
                await this.Server.SendTo(target.HostName, target.ClientId, parameters.Payload, cancellationToken);
                break;

            case Models.ClientAction.Broadcast:
                await this.Server.BroadCast(target.HostName, parameters.Payload, cancellationToken);
                break;
        }
    }
}
