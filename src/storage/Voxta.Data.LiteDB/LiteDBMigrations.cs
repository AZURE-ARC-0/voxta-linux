using LiteDB;

namespace Voxta.Data.LiteDB;

public class LiteDBMigrations
{
    private readonly ILiteDatabase _db;

    public LiteDBMigrations(ILiteDatabase db)
    {
        _db = db;
    }
    
    public Task MigrateAsync()
    {
        if (_db.UserVersion == 2)
        {
            Migrate_2_To_3();
            _db.UserVersion = 3;
        }

        return Task.CompletedTask;
    }

    private void Migrate_2_To_3()
    {
        var profileSettings = _db.GetCollection("ProfileSettings");
        foreach (var doc in profileSettings.FindAll())
        {
            doc.Remove("Services");
            profileSettings.Update(doc);
        }
    }
}