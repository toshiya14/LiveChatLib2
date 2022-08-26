using LiveChatLib2.Models;

namespace LiveChatLib2.Storage;

internal interface IBilibiliUserInfoStorage : IDisposable
{
    BilibiliUserInfo? PickUserInformation(string uid);
    void RecordUserInformation(BilibiliUserInfo user);
}