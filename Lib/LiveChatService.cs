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
using YamlDotNet.Core;
using IParser = LiveChatLib2.Parsers.IParser;

namespace LiveChatLib2;
public class LiveChatService
{
    private readonly ILogger log = LogManager.GetCurrentClassLogger();
    private ServicesLocator Services { get; set; }

    public LiveChatService()
    {
        LoggerConfig.InitLogger();
        this.Services = new ServicesLocator();
    }

    public async Task Deploy(CancellationToken cancellationToken)
    {
        try
        {
            var tasks = new List<Task>();

            // Initialize workers.
            tasks.Add(this.StartProcQueue(cancellationToken));

            // Initialize services.
            tasks.Add(this.StartServices(cancellationToken));

            // Initialize parsers.
            tasks.Add(this.StartParsers(cancellationToken));

            await Task.WhenAll(tasks);
            log.Info("Service terminated.");
        }
        catch (Exception ex)
        {
            log.Fatal($"Service terminated with failed: {ex.Message}\n{ex.StackTrace}");
        }
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
        var distributeService = this.Services.Resolve<DistributeService>();
        if (distributeService is null)
        {
            log.Fatal("Failed to resolve distribute service, the service could not be started.");
            return;
        }

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

        if (this.Services is null)
        {
            log.Fatal("ServiceLocator has not been initialized.");
            return;
        }

        var worker = this.Services.Resolve<IWorker<T>>();
        var queue = this.Services.Resolve<IMessageQueue<T>>();

        if (worker is null)
        {
            log.Fatal($"Failed to resolve {typeof(IWorker<T>).Name}.");
            return;
        }

        if (queue is null)
        {
            log.Fatal($"Failed to resolve {typeof(IMessageQueue<T>).Name}.");
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var message = queue.Dequeue();

                if (message == null)
                {
                    await Task.Delay(250, cancellationToken);
                    continue;
                }

                await worker.DoWork(message, cancellationToken);
            }
            catch (Exception ex)
            {
                log.Error($"Failed to process {typeof(T).Name} message: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    private async Task StartSingleParser<T>(CancellationToken cancellationToken) where T : class, IParser
    {
        log.Trace($"StartSingleParser for {typeof(T).Name} has been called.");

        try
        {
            var parser = this.Services.Resolve<T>();

            if (parser is null)
            {
                log.Fatal($"Could not resolve {typeof(T).Name}.");
                return;
            }

            await parser.Start(cancellationToken);
            log.Info($"Parser: {typeof(T).Name} started.");
        }
        catch (Exception ex)
        {
            log.Fatal($"Parser running failed: {ex.Message}\n=====\n{ex.StackTrace}\n=====");

            throw;
        }
    }
}
