namespace LiveChatLib2.Models.QueueMessages.WorkItems;

internal record SendWorkItem(
    string Source,
    ClientInfo Target,
    byte[] Payload
) : WorkItemBase(WorkItemAction.Send, Source);
