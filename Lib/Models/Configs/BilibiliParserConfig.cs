using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LiveChatLib2.Configs;

internal record BilibiliParserConfig
{
    public double? HeartBeatDuration { get; init; }
    public double? LostTimeoutThreshold { get; init; }
    public string? ProcessingRoomId { get; init; }
    public bool? ReconnectBadCommunication { get; init; }
    public int? ReconnectWaitTime { get; init; }
};
