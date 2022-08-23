using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LiveChatLib2.Utils;
internal static class ObjectSerializer
{
    private static readonly JsonSerializerSettings settings;
    static ObjectSerializer()
    {
        settings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
    }

    public static string ToJson(object data)
    {
        return JsonConvert.SerializeObject(data, settings);
    }

    public static T? FromJson<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json, settings)!;
    }

    public static byte[] ToJsonBinary(object data, Encoding? encode = null)
    {
        return (encode ?? Encoding.UTF8).GetBytes(ToJson(data));
    }

    public static T? FromJsonBinary<T>(byte[] jsonBinary, Encoding? encode = null) {
        return FromJson<T>((encode ?? Encoding.UTF8).GetString(jsonBinary));
    }
}
