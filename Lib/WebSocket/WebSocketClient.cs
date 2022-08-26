using NLog;
using WebSocketSharp;

namespace WebSocketSharp.Async;

internal class WebSocketClient : IDisposable
{
    private readonly bool autoReconnecting;
    private readonly int reconnectWaitTime;
    private bool manualClosed = false;

    private WebSocket Socket { get; init; }
    public string RemoteUrl { get; init; }

    private readonly ILogger log = LogManager.GetCurrentClassLogger();

    public WebSocketState ReadyState => this.Socket.ReadyState;
    public event EventHandler<MessageEventArgs> OnMessage = delegate { };
    public event EventHandler<CloseEventArgs> OnClose = delegate { };
    public event EventHandler<ErrorEventArgs> OnError = delegate { };
    public event EventHandler<EventArgs> OnOpen = delegate { };
    private void Socket_OnMessage(object? sender, MessageEventArgs e) => this.OnMessage?.Invoke(sender, e);
    private void Socket_OnClose(object? sender, CloseEventArgs e)
    {
        this.OnClose?.Invoke(sender, e);
        if (!manualClosed && autoReconnecting)
            _ = this.ReconnectAsync(default);
    }
    private void Socket_OnError(object? sender, ErrorEventArgs e)
    {
        this.OnError?.Invoke(sender, e);
        if (!manualClosed && autoReconnecting)
            _ = this.ReconnectAsync(default);
    }

    private void Socket_OnOpen(object? sender, EventArgs e)
    {
        this.OnOpen?.Invoke(sender, e);
    }

    public WebSocketClient(string remoteUrl, string[]? protocols = null, bool autoReconnecting = true, int reconnectWaitTime = 5000)
    {
        this.autoReconnecting = autoReconnecting;
        this.reconnectWaitTime = reconnectWaitTime;
        this.Socket = new WebSocket(remoteUrl, protocols);
        this.RemoteUrl = remoteUrl;
        this.Socket.OnMessage += this.Socket_OnMessage;
        this.Socket.OnClose += this.Socket_OnClose;
        this.Socket.OnError += this.Socket_OnError;
        this.Socket.OnOpen += this.Socket_OnOpen;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        log.Trace("Connecting...");
        this.manualClosed = false;
        await Task.Run(() =>
        {
            this.Socket.Connect();
            return;
        }, cancellationToken);
    }

    public async Task SendAsync(byte[] payload, CancellationToken cancellationToken)
    {
        log.Trace("Sending binary data.");
        await Task.Run(() =>
        {
            this.Socket.Send(payload);
        }, cancellationToken);
    }

    public async Task SendAsync(string data, CancellationToken cancellationToken)
    {
        log.Trace("Sending string data.");
        await Task.Run(() =>
        {
            this.Socket.Send(data);
        }, cancellationToken);
    }

    public void Close()
    {
        log.Trace("WebSocketClient manually closed.");
        this.manualClosed = true;
        this.Socket.Close();
    }

    public async Task ReconnectAsync(CancellationToken cancellationToken)
    {
        if (this.Socket == null)
        {
            log.Error("Could not processing Reconnecting: websocket not prepared.");
            await Task.Delay(this.reconnectWaitTime, cancellationToken);
            return;
        }
        else if (this.Socket.ReadyState is WebSocketState.Connecting or WebSocketState.Closing)
        {
            log.Error($"Could not processing Reconnecting: websocket is busy now. (STATE: {this.Socket.ReadyState})");
            await Task.Delay(this.reconnectWaitTime, cancellationToken);
            return;
        }

        if (this.Socket.ReadyState is WebSocketState.Open)
        {
            this.manualClosed = true;
            this.Socket.Close();
            await Task.Delay(this.reconnectWaitTime, cancellationToken);
        }

        await this.ConnectAsync(cancellationToken);
    }

    public void Dispose()
    {
        log.Trace("WebSocketClient disposed.");
        this.Close();
    }
}
