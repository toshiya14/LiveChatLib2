using LiveChatLib2.Exceptions;
using LiveChatLib2.Models;
using LiveChatLib2.Models.QueueMessages;
using LiveChatLib2.Queue;
using LiveChatLib2.Services;
using LiveChatLib2.Storage;
using LiveChatLib2.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using LiveChatLib2.Models.QueueMessages.WorkItems;
using System.Threading;

namespace LiveChatLib2.Workers;

internal class CrawlerWorker : IWorker<CrawlerWorkItem>
{
    public WorkItemAction[] ConsumerActions { get; }
    private IBilibiliUserInfoStorage Storage { get; }
    private IMessageQueue<SendWorkItem> SendQueue { get; }
    private IMessageQueue<RecordWorkItem> RecordQueue { get; }

    private readonly ILogger log = LogManager.GetCurrentClassLogger();

    public CrawlerWorker(
        IBilibiliUserInfoStorage storage,
        IMessageQueue<SendWorkItem> sendQueue,
        IMessageQueue<RecordWorkItem> recordQueue
    )
    {
        this.ConsumerActions = new[] { WorkItemAction.Crawl };
        this.Storage = storage;
        this.SendQueue = sendQueue;
        this.RecordQueue = recordQueue;
    }

    public async Task DoWork(CrawlerWorkItem parameters, CancellationToken cancellationToken)
    {
        switch (parameters.Source)
        {
            case "bilibili":
                // Check if the record already in the database.
                if (await this.ProcessBilibiliMessage(parameters, cancellationToken))
                {
                    // Prevent server block.
                    await Task.Delay(500, cancellationToken);
                }

                break;

            default:
                throw new InvalidOperationException($"Unknown source: {parameters.Source}");
        }
    }

    private async Task<bool> ProcessBilibiliMessage(CrawlerWorkItem parameters, CancellationToken cancellationToken)
    {
        var result = true;
        if (parameters is not BilibiliCrawlerWorkItem bcw)
        {
            throw new InvalidOperationException("The message ProcessBilibiliMessage are handling is not BilibiliCrawlerWorkItem.");
        }

        switch (bcw.TaskName)
        {
            case BilibiliCralwerTasks.User:
                if (bcw is not BilibiliUserCrawlerWorkItem bucw)
                {
                    throw new InvalidOperationException("The TaskName is User but the message ProcessBilibiliMessage are handling is not BilibiliUserCrawlerWorkItem.");
                }

                if (string.IsNullOrEmpty(bucw.UserId))
                {
                    log.Error($"Failed to fetch user info, Id is empty.\nOriginal: {JsonConvert.SerializeObject(bucw)}");
                    return false;
                }

                var indb = this.Storage.PickUserInformation(bucw.UserId);
                BilibiliUserInfo toSend;

                if (indb is not null)
                {
                    log.Warn($"No need to crawl this user, it is already in database, ignored.");
                    result = false;
                    toSend = indb;
                }
                else
                {
                    var user = await this.CrawlBilibiliUserInfo(bucw.UserId, cancellationToken);
                    if (user is null)
                    {
                        log.Error($"Faiiled to fetch user info with ID: {bucw.UserId}");
                        return false;
                    }

                    log.Info($"Successfully fetched user info with ID: {bucw.UserId}");

                    this.RecordQueue.Enqueue(
                        new RecordWorkItem(bucw.Source, RecordType.User, user)
                    );
                    toSend = user;
                }

                if (parameters.PostSendingTarget != null)
                {
                    this.SendQueue.Enqueue(
                        new SendWorkItem(
                            bucw.Source,
                            parameters.PostSendingTarget,
                            ObjectSerializer.ToJsonBinary(
                                new ClientMessageResponse("user-info", toSend)
                            )
                        )
                    );
                }

                break;

            default:
                throw new InvalidDataException($"Unknown TaskName: {bcw.TaskName}");
        }

        return result;
    }

    private async Task<BilibiliUserInfo?> CrawlBilibiliUserInfo(string uid, CancellationToken cancellationToken)
    {
        // Call api to get user information.
        var headers = new Dictionary<string, string>
        {
            ["User-Agent"] = @"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36",
            ["Referer"] = "http://m.bilibili.com",
            ["Origin"] = "http://m.bilibili.com",
        };

        // Post and get data from API.
        var result = await HttpRequests.DownloadString(
            url: "https://api.bilibili.com/x/space/acc/info?mid=" + uid,
            headers: headers,
            cancellationToken
        );
        var json = JToken.Parse(result);
        var face64 = "";

        if (json["data"]?.Type == JTokenType.Object)
        {
            // Download avatar.
            if (json["data"]!["face"] is not null)
            {
                var faceurl = json["data"]?["face"]?.ToString();
                if (faceurl != null)
                {
                    var facedata = await HttpRequests.DownloadBytes(faceurl, null, cancellationToken);
                    face64 = ImageHelper.ConvertToJpegBase64(facedata);
                }
            }
        }
        else
        {
            log.Error($"Failed to recognize json data.\nOriginal: {JsonConvert.SerializeObject(json)}");
            return null;
        }

        // Save the data.
        var user = new BilibiliUserInfo
        {
            Id = uid,
            BirthDay = json["data"]?["birthday"]?.ToString() ?? "保密",
            FaceUrl = json["data"]?["face"]?.ToString(),
            Face = face64,
            Level = json["data"]?["level_info"]?["current_level"]?.ToObject<int>() ?? -1,
            Name = json["data"]?["name"]?.ToString() ?? "<不知名先生>",
            Sex = json["data"]?["sex"]?.ToString() ?? "保密"
        };
        if (string.IsNullOrWhiteSpace(user.Id))
        {
            return null;
            // TODO: log warning.
        }

        this.RecordQueue.Enqueue(
            new RecordWorkItem(
                "bilibili",
                RecordType.User,
                user
            )
        );

        return user;
    }
}
