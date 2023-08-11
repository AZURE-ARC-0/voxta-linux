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
        
        if (_db.UserVersion == 4)
        {
            Migrate_4_To_5();
            _db.UserVersion = 5;
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
            }
        }
    }
    


    private void Migrate_4_To_5()
    {
        var messages = _db.GetCollection("ChatMessageData");
        foreach (var message in messages.FindAll())
        {
            var id = message["_id"];
            if (id.Type == BsonType.String)
            {
                messages.Delete(id);
                message["_id"] = new BsonValue(Guid.Parse(id.AsString));
                messages.Insert(message);
            }
        }
    }
}