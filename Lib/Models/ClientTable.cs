using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Server;

namespace LiveChatLib2.Models;
internal class ClientTable : ConcurrentDictionary<string, ClientInfo> { }
internal enum ClientAction
{
    Send,
    Broadcast
}
internal record ClientInfo {
    public ClientAction? Action { get; init; }
    public string HostName { get; init; }
    public string? ClientId { get; init; }

    public ClientInfo(ClientAction? action, string hostName, string? clientId = null)
    {
        this.Action = action;
        this.HostName = hostName;
        this.ClientId = clientId;
    }
}
