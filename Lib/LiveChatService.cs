using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveChatLib2.Models.QueueMessages;
using LiveChatLib2.Models.QueueMessages.WorkItems;
using LiveChatLib2.Queue;
using LiveChatLib2.Services;
using LiveChatLib2.Workers;
using NLog;

namespace LiveChatLib2;
internal class LiveChatService
{
    private readonly ILogger log = LogManager.GetCurrentClassLogger();
    public ServicesLocator Services { get; private set; }

    public async Task Deploy(CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        var services = new ServicesLocator();

        // Initialize workers.
        tasks.Add(this.StartProcQueue(cancellationToken));

        // Initialize service.
        tasks.Add(this.StartServices(cancellationToken));
    }

    private async Task StartProcQueue(CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            this.LoopProcWorkItemQueue<ClientMessage>(cancellationToken),
            this.LoopProcWorkItemQueue<CrawlerWorkItem>(cancellationToken),
            this.LoopProcWorkItemQueue<RecordWorkItem>(cancellationToken),
            this.LoopProcWorkItemQueue<SendWorkItem>(cancellationToken)
        );
    }

    private async Task StartServices(CancellationToken cancellationToken)
    {
        var distributeService = this.Services.Resolve<DistributeService>();
        await distributeService.Serve(cancellationToken);
    }

    private async Task LoopProcWorkItemQueue<T>(CancellationToken cancellationToken)
    {
        var worker = this.Services.Resolve<IWorker<T>>();
        var queue = this.Services.Resolve<IMessageQueue<T>>();

        while (!cancellationToken.IsCancellationRequested)
        {
            var message = queue.Dequeue();

            if (message == null)
            {
                await Task.Delay(250, cancellationToken);
                continue;
            }

            await worker.DoWork(message, cancellationToken);
        }
    }
}
