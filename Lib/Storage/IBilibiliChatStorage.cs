using LiveChatLib2.Models.MessageRecords;
using LiveChatLib2.Models.RemotePackages;

namespace LiveChatLib2.Storage;

internal interface IBilibiliChatStorage
{
    Task<IList<BilibiliMessageRecord>> PickLastestComments(int count, CancellationToken cancellationToken);
    Task RecordChatMessage(BilibiliMessageRecord package, CancellationToken cancellationToken);
    Task RecordRemotePackage(BilibiliRemotePackage package, CancellationToken cancellationToken);
}