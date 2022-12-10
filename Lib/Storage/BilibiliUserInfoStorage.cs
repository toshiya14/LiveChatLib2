using LiteDB;
using LiveChatLib2.Models;
using NLog;

namespace LiveChatLib2.Storage;

internal class BilibiliUserInfoStorage : StorageBase, IBilibiliUserInfoStorage
{
    private readonly ILogger log = LogManager.GetCurrentClassLogger();
    public BilibiliUserInfoStorage() {
        log.Trace("BilibiliUserInfoStorage Initialized.");
        this.InitializeDatabaseFolder();
    }

    public BilibiliUserInfo? PickUserInformation(string uid)
    {
        lock (this.UserDatabasePath)
        {
            using var db = new LiteDatabase(base.UserDatabasePath);
            var users = db.GetCollection<BilibiliUserInfo>("users");
            var result = users.FindOne(x => x.Id == uid);
            return result;
        }
    }

    public void RecordUserInformation(BilibiliUserInfo user)
    {
        lock (this.UserDatabasePath)
        {
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
}
