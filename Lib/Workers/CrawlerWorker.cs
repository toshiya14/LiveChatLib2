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
    private readonly ILogger log = LogManager.GetCurrentClassLogger();

    public CrawlerWorker(
        IMessageQueue<WorkItemBase> queue
    )
    {
        this.Queue = queue;
        this.ConsumerActions = new[] { WorkItemAction.Crawl };
    }

    private IMessageQueue<WorkItemBase> Queue { get; }

    public async Task DoWork(CrawlerWorkItem parameters, CancellationToken cancellationToken)
    {
        switch (parameters.Source)
        {
            case "bilibili":
                await this.ProcessBilibiliMessage(parameters, cancellationToken);
                break;

            default:
                throw new InvalidOperationException($"Unknown source: {parameters.Source}");
        }
    }

    private async Task ProcessBilibiliMessage(CrawlerWorkItem parameters, CancellationToken cancellationToken)
    {
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

                var user = await this.CrawlBilibiliUserInfo(bucw.UserId, cancellationToken);
                if (user is null) 
                {
                    log.Warn($"Faiiled to fetch user info with ID: {bucw.UserId}");
                    return;
                }

                log.Info($"Successfully fetched user info with ID: {bucw.UserId}");

                this.Queue.Enqueue(
                    new RecordWorkItem(bucw.Source, RecordType.User, user)
                );

                if (parameters.PostSendingTarget != null)
                {
                    this.Queue.Enqueue(
                        new SendWorkItem(
                            bucw.Source,
                            parameters.PostSendingTarget,
                            ObjectSerializer.ToJsonBinary(
                                new ClientMessageResponse("user-info", user!)
                            )
                        )
                    );
                }

                break;

            default:
                throw new InvalidDataException($"Unknown TaskName: {bcw.TaskName}");
        }
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

        // Download avatar.
        if (json["data"]?["face"] != null)
        {
            var faceurl = json["data"]?["face"]?.ToString();
            if (faceurl != null)
            {
                var facedata = await HttpRequests.DownloadBytes(faceurl, null, cancellationToken);
                face64 = ImageHelper.ConvertToJpegBase64(facedata);
            }
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

        this.Queue.Enqueue(
            new RecordWorkItem(
                "bilibili",
                RecordType.User,
                user
            )
        );

        return user;
    }
}
