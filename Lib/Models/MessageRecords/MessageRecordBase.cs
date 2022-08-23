using Newtonsoft.Json;

namespace LiveChatLib2.Models.MessageRecords;

internal abstract record LocalMessageBase
{
    [JsonProperty(PropertyName = "_id")]
    public string? Id { get; set; }

    [JsonProperty(PropertyName = "avatar")]
    public string? Avatar { get; set; }

    [JsonProperty(PropertyName = "sender")]
    public string? SenderName { get; set; }

    [JsonProperty(PropertyName = "uid")]
    public string? SenderId { get; set; }

    [JsonProperty(PropertyName = "comment")]
    public string? Comment { get; set; }

    [JsonProperty(PropertyName = "time")]
    public DateTimeOffset? ReceiveTime { get; set; }

    [JsonIgnore]
    public string? RawData { get; set; }

    [JsonProperty(PropertyName = "source")]
    public abstract string? Source { get; }
}
