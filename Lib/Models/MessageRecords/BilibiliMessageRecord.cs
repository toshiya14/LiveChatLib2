using Newtonsoft.Json;

namespace LiveChatLib2.Models.MessageRecords;

internal record BilibiliMessageRecord : LocalMessageBase
{
    public override string? Source => "bilibili";

    [JsonProperty(PropertyName = "type")]
    public string? Type { get; set; }

    [JsonProperty(PropertyName = "giftName")]
    public string? GiftName { get; set; }

    [JsonProperty(PropertyName = "giftCount")]
    public int? GiftCount { get; set; }

    [JsonProperty(PropertyName = "meta")]
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}
