using LiteDB;
using LiveChatLib2.Models;

namespace LiveChatLib2.Storage;

internal class BilibiliUserInfoStorage : StorageBase, IBilibiliUserInfoStorage
{
    public BilibiliUserInfo? PickUserInformation(string uid)
    {
        base.InitializeDatabaseFolder();
        using var db = new LiteDatabase(base.UserDatabasePath);
        var users = db.GetCollection<BilibiliUserInfo>("users");
        var result = users.FindOne(x => x.Id == uid);
        return result;
    }

    public void RecordUserInformation(BilibiliUserInfo user)
    {
        base.InitializeDatabaseFolder();
        using var db = new LiteDatabase(base.UserDatabasePath);
        var users = db.GetCollection<BilibiliUserInfo>("users");
        var results = users.Find(x => x.Id == user.Id);
        if (!results.Any())
        {
            users.Insert(user);
            users.EnsureIndex(x => x.Id);
            users.EnsureIndex(x => x.Name);
        }
        else
        {
            var toUpdate = results.First();
            toUpdate.BirthDay = user.BirthDay;
            toUpdate.Face = user.Face;
            toUpdate.FaceUrl = user.FaceUrl;
            toUpdate.Level = user.Level;
            toUpdate.Name = user.Name;
            toUpdate.Sex = user.Sex;
            users.Update(toUpdate);
        }
    }
}
