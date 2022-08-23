namespace LiveChatLib2.Models.QueueMessages.WorkItems;

internal record CrawlerWorkItem(
    string Source,
    ClientInfo? PostSendingTarget = null
) : WorkItemBase(WorkItemAction.Crawl, Source);

internal enum BilibiliCralwerTasks
{
    User
}

internal abstract record BilibiliCrawlerWorkItem(
    BilibiliCralwerTasks TaskName,
    ClientInfo? PostSendingTarget = null
) :CrawlerWorkItem("bilibili", PostSendingTarget);

internal record BilibiliUserCrawlerWorkItem(
    string UserId,
    ClientInfo? PostSendingTarget = null
) : BilibiliCrawlerWorkItem(BilibiliCralwerTasks.User, PostSendingTarget);
