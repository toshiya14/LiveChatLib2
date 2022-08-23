using Newtonsoft.Json;

namespace LiveChatLib2.Models
{
    internal record BilibiliUserInfo
    {
        [JsonProperty(PropertyName = "mid")]
        public string? Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string? Name { get; set; }

        [JsonProperty(PropertyName = "sex")]
        public string? Sex { get; set; }

        [JsonProperty(PropertyName = "face_url")]
        public string? FaceUrl { get; set; }

        [JsonProperty(PropertyName = "face")]
        public string? Face { get; set; }

        [JsonProperty(PropertyName = "birth")]
        public string? BirthDay { get; set; }

        [JsonProperty(PropertyName = "lv")]
        public int? Level { get; set; }

        [JsonProperty(PropertyName = "uptime")]
        public DateTimeOffset? LastUpdateTime { get; set; }
    }
}
