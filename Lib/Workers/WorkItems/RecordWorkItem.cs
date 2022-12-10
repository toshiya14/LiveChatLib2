namespace LiveChatLib2.Models.QueueMessages.WorkItems;

internal enum RecordType
{
    User,
    Chat,
    Package
}

internal record RecordWorkItem(
    string Source,
    RecordType Type,
    object Record
) : WorkItemBase(WorkItemAction.Record, Source);