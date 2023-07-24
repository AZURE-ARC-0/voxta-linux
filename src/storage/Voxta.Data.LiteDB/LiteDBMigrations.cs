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
        if (_db.UserVersion == 1)
        {
            Migrate_1_To_2();
            _db.UserVersion = 2;
        }

        return Task.CompletedTask;
    }

    private void Migrate_1_To_2()
    {
    }
}