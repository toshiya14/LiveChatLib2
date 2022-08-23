using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketSharp.Async;
internal class WebSocketServer : IDisposable
{
    private Server.WebSocketServer Proxy { get; set; }

    public WebSocketServer(int port)
    {
        this.Proxy = new Server.WebSocketServer(port);
    }

    public void AddWebSocketService<T>(string route, Action<T>? creator = null) where T : Server.WebSocketBehavior, new() => this.Proxy.WebSocketServices.AddService(route, creator);

    public Server.WebSocketSessionManager GetSession(string route) => this.Proxy.WebSocketServices[route].Sessions;

    public Server.WebSocketServiceHost GetHost(string route) => this.Proxy.WebSocketServices[route];

    public async Task SendTo(string route, string clientId, byte[] payload, CancellationToken cancellationToken)
    {
        var session = this.GetSession(route);
        await Task.Run(() =>
        {
            session.SendTo(payload, clientId);
        }, cancellationToken);
    }

    public async Task BroadCast(string route, byte[] payload, CancellationToken cancellationToken)
    {
        var session = this.GetSession(route);
        await Task.Run(() =>
        {
            session.Broadcast(payload);
        }, cancellationToken);
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            this.Proxy.Start();
        }, cancellationToken);
    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            this.Proxy.Stop();
        }, cancellationToken);
    }

    public void Dispose()
    {
        this.Proxy.Stop();
    }
}
