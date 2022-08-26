using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveChatLib2.Models;
using LiveChatLib2.Models.QueueMessages;
using LiveChatLib2.Queue;
using Newtonsoft.Json;
using NLog;
using WebSocketSharp;
using WebSocketSharp.Async;
using WebSocketBehavior = WebSocketSharp.Server.WebSocketBehavior;

namespace LiveChatLib2.Services;

internal class DistributeService : IDisposable
{
    private WebSocketServer Server { get; set; }

    private ILogger log = LogManager.GetCurrentClassLogger();

    public DistributeService(
        WebSocketServer server,
        IMessageQueue<ClientMessage> queue,
        ClientTable clientTable
    )
    {
        this.Server = server;
        this.Server.AddWebSocketService<DistributeServiceApp>(
            DistributeServiceApp.ROUTE,
            () =>
            {
                var app = new DistributeServiceApp();
                app.Initialize(queue, clientTable);
                return app;
            }
        );

        log.Info("DistributeService has been initialized.");
    }

    public async Task Serve(CancellationToken cancellationToken)
    {
        await this.Server.Start(cancellationToken);
    }

    public void Dispose()
    {
        this.Server.Dispose();
    }
}

internal class DistributeServiceApp : WebSocketBehavior
{
    public const string ROUTE = "/app";
    private readonly ILogger log = LogManager.GetCurrentClassLogger();

    private IMessageQueue<ClientMessage>? Queue { get; set; }

    private ClientTable? ClientTable { get; set; }

    public void Initialize(
        IMessageQueue<ClientMessage> queue,
        ClientTable clientTable
    )
    {
        this.Queue = queue;
        this.ClientTable = clientTable;
    }

    protected override void OnOpen()
    {
        if (this.ClientTable == null)
        {
            throw new InvalidOperationException("DistributeService has not been initialized.");
        }

        log.Info($"New session open @ {ROUTE}: clientId={this.ID}");
        this.ClientTable[this.ID] = new ClientInfo(null, this.ID, ROUTE);
        base.OnOpen();
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        if (this.Queue == null)
        {
            throw new InvalidOperationException("DistributeService has not been initialized.");
        }

        if (this.ClientTable == null)
        {
            throw new InvalidOperationException("DistributeService has not been initialized.");
        }

        // Reply ping inmediately.
        if (e.IsPing || (e.IsText && e.Data.Equals("ping", StringComparison.OrdinalIgnoreCase)))
        {
            this.Sessions.SendTo(this.ID, "pong");
            base.OnMessage(e);
            return;
        }

        if (e.IsBinary)
        {
            log.Warn($"Received binary package from client {this.ID} @ {ROUTE}, ignored.");
            return;
        }

        // Try to resolve json from the text.
        if (e.IsText)
        {
            try
            {
                var json = JsonConvert.DeserializeObject<ClientMessage>(e.Data);

                if (json == null)
                {
                    log.Warn($"Failed to deserialize package (get null):\n=====\n{e.Data}\n=====");
                    return;
                }

                this.Queue.Enqueue(json with { ClientInfo = new ClientInfo(null, ROUTE, this.ID) });
                base.OnMessage(e);
            }
            catch (Exception ex)
            {
                log.Warn($"Failed to deserialize package (exception: {ex.Message}):\n=====\n{e.Data}\n=====");
            }

            return;
        }

        log.Warn($"Received unrecognized package from client {this.ID} @ {ROUTE}, ignored.");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        if (this.ClientTable == null)
        {
            throw new InvalidOperationException("DistributeService has not been initialized.");
        }

        log.Info($"Disconnected @ {ROUTE}: clientId={this.ID}, code={e.Code}, reason={e.Reason}");
        this.ClientTable.Remove(this.ID, out _);
        base.OnClose(e);
    }

    protected override void OnError(WebSocketSharp.ErrorEventArgs e)
    {
        if (this.ClientTable == null)
        {
            throw new InvalidOperationException("DistributeService has not been initialized.");
        }

        log.Error($"Error on @ {ROUTE}: clientId={this.ID}, message={e.Message}");
        this.ClientTable.Remove(this.ID, out _);
        base.OnError(e);
    }
}
