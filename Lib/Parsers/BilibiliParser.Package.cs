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
    protected static async Task SendAuthPackage(WebSocketClient socket, string uid, string roomid, string token, CancellationToken cancellationToken)
    {
        var body = new
        {
            uid = int.Parse(uid),
            roomid = int.Parse(roomid),
            protover = 2,
            platform = "web",
            clientver= "1.10.6",
            key = token
        };
        var package = new BilibiliRemotePackage(BilibiliMessageType.Auth, JsonConvert.SerializeObject(body, Formatting.None, new JsonSerializerSettings{NullValueHandling = NullValueHandling.Ignore}));
        log.Debug($"AuthPackage: {JsonConvert.SerializeObject(package)}");
        await socket.SendAsync(package.ToByteArray(), cancellationToken);
    }

    protected static async Task SendHeartBeat(WebSocketClient socket, CancellationToken cancellationToken)
    {
        var package = new BilibiliRemotePackage(BilibiliMessageType.ClientHeart, "{}");
        log.Debug($"HeartBeat: {JsonConvert.SerializeObject(package)}");
        await socket.SendAsync(package.ToByteArray(), cancellationToken);
    }

    protected IEnumerable<BilibiliRemotePackage> ParsePackagesFromBinary(byte[] buffer)
    {
        var list = new List<BilibiliRemotePackage>();
        var parts = this.SplitBuffer(buffer);

        foreach (var p in parts)
        {
            if (p == null)
            {
                // If one of the package is broken, stop parsing.
                break;
            }

            list.AddRange(BilibiliRemotePackage.LoadFromByteArray(p));
        }

        return list;
    }

    protected BilibiliMessageRecord? ConvertPackageToMessage(BilibiliRemotePackage package)
    {

        if (package.MessageType is BilibiliMessageType.Auth)
        {
            log.Warn($"Auth package should not be package from server, please check.");
            return null;
        }

        if (package.MessageType is BilibiliMessageType.ClientHeart)
        {
            log.Warn($"ClientHeart package should not be package from server, please check.");
            return null;
        }

        var message = new BilibiliMessageRecord
        {
            Id = package.Id,
            RawData = package.Content,
            ReceiveTime = DateTimeOffset.Now
        };
        var content = package.Content.Trim();

        JToken? json = null, data = null;
        string? cmd = null;
        try
        {
            json = JToken.Parse(content);
            cmd = json["cmd"]?.ToString().ToUpper()!;
            data = json["data"]!;
        }
        catch (JsonReaderException)
        {
            log.Warn($"Incoming package is not a json package.");
            if (package.MessageType is not
                BilibiliMessageType.Renqi or
                BilibiliMessageType.ServerHeart)
            {
                return null;
            }
        }

        switch (package.MessageType)
        {
            case BilibiliMessageType.Renqi:
                var renqi = package.Body?.ByteToInt32(true);
                message.Type = "renqi";
                message.Metadata["renqi"] = renqi?.ToString()!;
                message.SenderName = "server";
                break;

            case BilibiliMessageType.ServerHeart:
                message.Type = "heartbeat";
                message.SenderName = "server";
                break;

            case BilibiliMessageType.Command:
                if (cmd is null)
                {
                    log.Error($"Failed to load cmd from command type message: {package.Id}");
                    return null;
                }

                message.Metadata["cmd"] = cmd;

                switch (cmd)
                {
                    case "WELCOME_GUARD":
                        if (data is null)
                        {
                            log.Error($"Failed to load data from command type message: {package.Id}");
                            return null;
                        }

                        message.SenderId = data["uid"]?.ToString()!;
                        message.Metadata["uname"] = data["username"]?.ToString()!;
                        message.Metadata["guard_level"] = data["guard_level"]?.ToObject<int>().ToString()!;
                        message.SenderName = message.Metadata["uname"];
                        message.Type = "welcome";
                        message.SenderId = data["uid"]?.ToString();
                        break;

                    case "WELCOME":
                        if (data is null)
                        {
                            log.Error($"Failed to load data from command type message: {package.Id}");
                            return null;
                        }

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
                        if (data is null)
                        {
                            log.Error($"Failed to load data from command type message: {package.Id}");
                            return null;
                        }

                        message.SenderId = data["uid"]?.ToString()!;
                        message.Metadata["uname"] = data["uname"]?.ToString()!;
                        message.SenderName = message.Metadata["uname"];
                        message.Type = "welcome";
                        message.SenderId = data["uid"]?.ToString();
                        break;

                    case "SEND_GIFT":
                        if (data is null)
                        {
                            log.Error($"Failed to load data from command type message: {package.Id}");
                            return null;
                        }

                        message.SenderId = data["uid"]?.ToString()!;
                        message.GiftName = data["giftName"]?.ToString()!;
                        message.GiftCount = data["num"]?.ToObject<int>();
                        message.Metadata["price"] = data["price"]?.ToString()!;
                        message.SenderName = data["uname"]?.ToString()!;
                        message.Type = "gift";
                        break;

                    case "PREPARING":
                        message.Metadata["roomid"] = json!["roomid"]?.ToString()!;
                        message.SenderName = "server";
                        message.Metadata["action"] = "CLOSE";
                        message.Type = "system";
                        break;

                    case "LIVE":
                        message.Metadata["roomid"] = json!["roomid"]?.ToString()!;
                        message.SenderName = "server";
                        message.Metadata["action"] = "OPEN";
                        message.Type = "system";
                        break;

                    case "DANMU_MSG":
                        var info = json?["info"];

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
                        message.Type = "system";
                        break;

                    case "STOP_LIVE_ROOM_LIST":
                        message.SenderName = "server";
                        message.Type = "system";
                        message.Metadata["room_id_list"] = string.Join(",", data["room_id_list"]?.ToObject<string[]>()!);
                        break;

                    case "ONLINE_RANK_COUNT":
                        if (data is null)
                        {
                            log.Error($"Failed to load data from command type message: {package.Id}");
                            return null;
                        }

                        message.SenderName = "server";
                        message.Type = "system";
                        message.SenderName = "server";
                        message.Metadata["count"] = data["count"]?.ToString()!;
                        break;

                    case "HOT_RANK_CHANGED_V2":
                        if (data is null)
                        {
                            log.Error($"Failed to load data from command type message: {package.Id}");
                            return null;
                        }

                        message.SenderName = "server";
                        message.Type = "system";
                        message.SenderName = "server";
                        message.Metadata["area_name"] = data["area_name"]?.ToString()!;
                        message.Metadata["countdown"] = data["countdown"]?.ToString()!;
                        message.Metadata["icon"] = data["icon"]?.ToString()!;
                        message.Metadata["rank"] = data["rank"]?.ToString()!;
                        message.Metadata["rank_desc"] = data["rank_desc"]?.ToString()!;
                        message.Metadata["trend"] = data["trend"]?.ToString()!;
                        break;

                    case "WATCHED_CHANGE":
                        if (data is null)
                        {
                            log.Error($"Failed to load data from command type message: {package.Id}");
                            return null;
                        }

                        message.SenderName = "server";
                        message.Type = "renqi";
                        message.Metadata["renqi"] = data["num"]?.ToString()!;
                        message.SenderName = "server";
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

    private IEnumerable<byte[]> SplitBuffer(byte[] buffer)
    {
        using var ms = new MemoryStream(buffer);
        ms.Seek(0, SeekOrigin.Begin);
        using var reader = new BinaryReader(ms);
        var list = new List<byte[]>();

        while (ms.Position < ms.Length)
        {
            // Check the remaining length is greater than 4
            if (ms.Length - ms.Position < 4)
            {
                log.Error("Binary frame broken, the package size smaller than 4 bytes, could read meta data.");
                break;
            }

            var length = reader.ReadBytes(4).ByteToInt32(true);
            if (length > buffer.Length)
            {
                log.Error("Binary frame broken, package size:" + buffer.Length + ", size in header:" + length);
                break;
            }

            ms.Seek(-4, SeekOrigin.Current);
            var pack = reader.ReadBytes(length);
            if (pack != null)
            {
                list.Add(pack);
            }
        }

        return list;
    }
}
