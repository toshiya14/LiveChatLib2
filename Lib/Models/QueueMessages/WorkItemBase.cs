namespace LiveChatLib2.Models.QueueMessages;

internal enum WorkItemAction
{
    Crawl,
    Record,
    Send
};

internal static class WorkItemSources {
    public const string BILIBILI = "bilibili";
};

internal abstract record WorkItemBase(
    WorkItemAction Action,
    string Source
)
{
    public WorkItemAction Action { get; init; } = Action;
    public string Source { get; init; } = Source;
}
