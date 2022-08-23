namespace LiveChatLib2.Storage;

internal abstract class StorageBase
{
    public readonly string DatabaseHomeFolder;
    public readonly string UserDatabasePath;
    public readonly string ChatLogDatabasePath;
    public readonly string SampleDatabasePath;

    public StorageBase()
    {
        DatabaseHomeFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"db");
        UserDatabasePath = Path.Combine(DatabaseHomeFolder, @"user.db");
        ChatLogDatabasePath = Path.Combine(DatabaseHomeFolder, @"chatlog/" + DateTime.Now.ToString("yyyy-MM-dd") + @".db");
        SampleDatabasePath = Path.Combine(DatabaseHomeFolder, @"samples/" + DateTime.Now.ToString("yyyy-MM-dd") + @".db");
    }

    public void InitializeDatabaseFolder()
    {
        Directory.CreateDirectory(DatabaseHomeFolder);
    }
}
