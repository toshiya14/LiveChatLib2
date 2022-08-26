using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveChatLib2.Models.QueueMessages;
using LiveChatLib2.Models.QueueMessages.WorkItems;
using LiveChatLib2.Parsers;
using LiveChatLib2.Queue;
using LiveChatLib2.Services;
using LiveChatLib2.Utils;
using LiveChatLib2.Workers;
using NLog;

namespace LiveChatLib2;
public class LiveChatService
{
    private readonly ILogger log = LogManager.GetCurrentClassLogger();
    private ServicesLocator Services { get; set; }

    public LiveChatService()
    {
        this.Services = new ServicesLocator();
        LoggerConfig.InitLogger();
    }

    public async Task Deploy(CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        // Initialize workers.
        tasks.Add(this.StartProcQueue(cancellationToken));

        // Initialize services.
        tasks.Add(this.StartServices(cancellationToken));

        // Initialize parsers.
        tasks.Add(this.StartParsers(cancellationToken));

        await Task.WhenAll(tasks);
        Console.WriteLine("Service terminated.");
    }

    private async Task StartProcQueue(CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            this.LoopProcessingQueue<ClientMessage>(cancellationToken),
            this.LoopProcessingQueue<CrawlerWorkItem>(cancellationToken),
            this.LoopProcessingQueue<RecordWorkItem>(cancellationToken),
            this.LoopProcessingQueue<SendWorkItem>(cancellationToken)
        );
    }

    private async Task StartServices(CancellationToken cancellationToken)
    {
        var distributeService = this.Services.ResolveRequired<DistributeService>();
        await distributeService.Serve(cancellationToken);
    }

    private async Task StartParsers(CancellationToken cancellationToken)
    {
        // If there are multiple parsers.
        //await Task.WhenAll(
        //    this.StartSingleParser<IBilibiliParser>(cancellationToken)
        //);

        // If there is only one parser.
        await this.StartSingleParser<IBilibiliParser>(cancellationToken);
    }

    private async Task LoopProcessingQueue<T>(CancellationToken cancellationToken)
    {
        log.Trace($"Start processing worker item queue: {typeof(T).Name}.");
        var worker = this.Services.ResolveRequired<IWorker<T>>();
        var queue = this.Services.ResolveRequired<IMessageQueue<T>>();

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

    private async Task StartSingleParser<T>(CancellationToken cancellationToken) where T : class, IParser
    {
        log.Trace($"StartSingleParser for {typeof(T).Name} has been called.");

        if (this.Services == null)
        {
            throw new Exception("ServiceLocator has not been initialized.");
        }

        var parser = this.Services.ResolveRequired<T>();
        try
        {
            await parser.Start(cancellationToken);
            log.Info($"Parser: {typeof(T).Name} started.");
        }
        catch (Exception ex)
        {
            log.Fatal($"Parser running failed: {ex.Message}\n=====\n{ex.StackTrace}\n=====");
            parser.Dispose();
            throw;
        }
    }
}
