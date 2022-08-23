using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveChatLib2.Models.QueueMessages;
internal record ClientMessage
{
    public ClientInfo? ClientInfo { get; set; }

    public string? Processor { get; set; }

    public string? Action { get; set; }

    public Dictionary<string, object>? Parameters { get; set; }
}