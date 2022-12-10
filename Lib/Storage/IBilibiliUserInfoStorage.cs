using LiveChatLib2.Models;

namespace LiveChatLib2.Storage;

internal interface IBilibiliUserInfoStorage
{
    BilibiliUserInfo? PickUserInformation(string uid);
    void RecordUserInformation(BilibiliUserInfo user);
}