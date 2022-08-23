using LiveChatLib2.Utils;
using WebSocketSharp.Async;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LiveChatLib2.Models.MessageRecords;
using LiveChatLib2.Models.RemotePackages;
using NLog;

namespace LiveChatLib2.Parsers;

internal partial class BilibiliParser
{
    protected static BilibiliRemotePackage MakeAuthPackage(string uid, string roomid, string token)
    {
        var body = new
        {
            uid,
            roomid,
            protover = 2,
            platform = "web",
            clientver= "1.10.6",
            key = token
        };
        var package = new BilibiliRemotePackage(BilibiliMessageType.Auth, JsonConvert.SerializeObject(body, Newtonsoft.Json.Formatting.None,new JsonSerializerSettings{NullValueHandling = NullValueHandling.Ignore}));
        return package;
    }

    protected static BilibiliRemotePackage MakeHeartBeat()
    {
        var package = new BilibiliRemotePackage(BilibiliMessageType.ClientHeart, "{}");
        return package;
    }

    protected static async Task SendHeartBeat(WebSocketClient socket, CancellationToken cancellationToken)
    {
        //var package = new BilibiliRemotePackage(BilibiliMessageType.ClientHeart, "");
        //var data = package.Body;
        //socket.SendAsync(data, null);

        var package = MakeHeartBeat();
        await socket.SendAsync(package.ToByteArray(), cancellationToken);
    }

    protected static IEnumerable<BilibiliRemotePackage> ParsePackagesFromBinary(byte[] buffer)
    {
        var parts = SplitBuffer(buffer);

        foreach (var p in parts)
        {
            foreach (var package in BilibiliRemotePackage.LoadFromByteArray(p))
            {
                yield return package;
            }
        }
    }

    protected BilibiliMessageRecord? ConvertPackageToMessage(BilibiliRemotePackage package)
    {
        var message = new BilibiliMessageRecord
        {
            Id = package.Id,
            RawData = package.Content
        };
        var content = package.Content.Trim();
        var json = JToken.Parse(content);
        var data = json["data"];

        switch (package.MessageType)
        {
            case BilibiliMessageType.Renqi:
                var renqi = package.Body?.ByteToInt32(true);
                message.Avatar = string.Empty;
                message.Type = "renqi";
                message.ReceiveTime = DateTimeOffset.Now;
                message.Metadata["renqi"] = renqi?.ToString()!;
                message.SenderId = string.Empty;
                message.SenderName = "server";
                break;

            case BilibiliMessageType.ServerHeart:
                message.Avatar = string.Empty;
                message.Type = "heartbeat";
                message.ReceiveTime = DateTimeOffset.Now;
                message.SenderId = string.Empty;
                message.SenderName = "server";
                break;

            case BilibiliMessageType.Command:
                if (data == null)
                {
                    log.Error($"Failed to load data from command type message: {package.Id}");
                    return null;
                }

                var cmd = json["cmd"]?.ToString().ToUpper();
                switch (cmd)
                {
                    case "WELCOME_GUARD":
                        message.SenderId = data["uid"]?.ToString()!;
                        message.Metadata["uname"] = data["username"]?.ToString()!;
                        message.Metadata["guard_level"] = data["guard_level"]?.ToObject<int>().ToString()!;
                        message.SenderName = message.Metadata["uname"];
                        message.Type = "welcome";
                        message.SenderId = data["uid"]?.ToString();
                        break;

                    case "WELCOME":
                        message.Metadata["cmd"] = "WELCOME";
                        message.SenderId = data["uid"]?.ToString()!;
                        message.Metadata["uname"] = data["uname"]?.ToString()!;
                        message.Metadata["is_admin"] = data["isadmin"]?.ToObject<bool>().ToString() ?? data["is_admin"]?.ToObject<bool>().ToString() ?? (data["is_admin"]?.ToObject<int>() == 1).ToString() ?? "false";
                        message.Metadata["is_vip"] = (data["vip"] != null && data["vip"]?.ToObject<int>() == 1).ToString();
                        message.Metadata["is_svip"] = (data["svip"] != null && data["svip"]?.ToObject<int>() == 1).ToString();
                        message.SenderName = message.Metadata["uname"];
                        message.Type = "welcome";
                        message.SenderId = data["uid"]?.ToString();
                        break;

                    case "INTERACT_WORD":
                        message.Metadata["cmd"] = "INTERACT_WORD";
                        message.SenderId = data["uid"]?.ToString()!;
                        message.Metadata["uname"] = data["uname"]?.ToString()!;
                        message.SenderName = message.Metadata["uname"];
                        message.Metadata["face_url"] = message.Metadata["face"];
                        message.Type = "welcome";
                        message.SenderId = data["uid"]?.ToString();
                        break;

                    case "SEND_GIFT":
                        message.SenderId = data["uid"]?.ToString()!;
                        message.GiftName = data["giftName"]?.ToString()!;
                        message.GiftCount = data["num"]?.ToObject<int>();
                        message.Metadata["price"] = data["price"]?.ToString()!;
                        message.SenderName = data["uname"]?.ToString()!;
                        message.Type = "gift";
                        break;

                    case "PREPARING":
                        message.Metadata["roomid"] = json["roomid"]?.ToString()!;
                        message.SenderName = "server";
                        message.Metadata["action"] = "CLOSE";
                        message.Type = "system";
                        break;

                    case "LIVE":
                        message.Metadata["roomid"] = json["roomid"]?.ToString()!;
                        message.SenderName = "server";
                        message.Metadata["action"] = "OPEN";
                        message.Type = "system";
                        break;

                    case "DANMU_MSG":
                        var info = json["info"];
                        if (info == null)
                        {
                            log.Error($"Failed to load info from DANMU_MSG type message: {package.Id}");
                            return null;
                        }

                        var ts = info[0]?[4]?.ToString();
                        message.Metadata["ts"] = ts!;

                        message.SenderId = info[2]?[0]?.ToString();
                        message.Metadata["flag1"] = info[2]?[2]?.ToObject<int>().ToString()!;
                        message.Metadata["flag2"] = info[2]?[3]?.ToObject<int>().ToString()!;
                        message.Metadata["flag3"] = info[2]?[4]?.ToObject<int>().ToString()!;
                        message.SenderName = info[2]?[1]?.ToString()!;
                        message.Type = "comment";
                        message.Comment = info[1]?.ToString();
                        break;

                    case "NOTICE_MSG":
                        message.SenderName = "server";
                        message.Metadata["cmd"] = "NOTICE_MSG";
                        message.Type = "system";
                        break;

                    default:
                        message.Type = "unknown";
                        log.Warn($"Got unknown command type({cmd}) message: {package.Id}");
                        break;
                }

                break;

            default:
                message.Type = "unknown";
                log.Warn($"Got unknown type message: {package.Id}");
                break;
        }

        return message;
    }

    private static IEnumerable<byte[]> SplitBuffer(byte[] buffer)
    {
        using var ms = new MemoryStream(buffer);
        ms.Seek(0, SeekOrigin.Begin);
        using var reader = new BinaryReader(ms);
        while (ms.Position < ms.Length)
        {
            byte[]? pack = null;
            // Check the remaining length is greater than 4
            if (ms.Length - ms.Position < 4)
            {
                throw new Exception("Binary frame broken, the package size smaller than 4 bytes, could read meta data.");
            }

            var length = reader.ReadBytes(4).ByteToInt32(true);
            if (length > buffer.Length)
            {
                throw new Exception("Binary frame broken, package size:" + buffer.Length + ", size in header:" + length);
            }

            ms.Seek(-4, SeekOrigin.Current);
            pack = reader.ReadBytes(length);
            if (pack != null)
            {
                yield return pack;
            }
        }
    }
}
