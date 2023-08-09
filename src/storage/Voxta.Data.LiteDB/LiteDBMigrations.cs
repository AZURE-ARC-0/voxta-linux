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
        if (_db.UserVersion < 3)
        {
            Migrate_3_To_4();
            _db.UserVersion = 4;
        }

        return Task.CompletedTask;
    }

    private void Migrate_3_To_4()
    {
        var characters = _db.GetCollection("Character");
        foreach (var character in characters.FindAll())
        {
            var id = character["_id"];
            if (id.Type == BsonType.String)
            {
                characters.Delete(id);
                // id = new BsonValue(Guid.Parse(id.AsString));
                // character["_id"] = id;
                // if (!characters.FindById(id))
                //     characters.Insert(character);
            }
        }
    }
}