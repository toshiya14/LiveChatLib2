using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveChatLib2.Configs;
internal record LiveChatLibConfig
{
    public int? DistributorPort { get; init; }
}
