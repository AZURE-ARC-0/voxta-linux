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
        
        if (_db.UserVersion == 5)
        {
            Migrate_5_To_6();
            _db.UserVersion = 6;
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

    private void Migrate_5_To_6()
    {
        var messages = _db.GetCollection("ChatMessageData");
        messages.DeleteAll();
        UpdateCollection("OobaboogaSettings");
    }

    private void UpdateCollection(string name)
    {
        var collection = _db.GetCollection(name);
        foreach (var settings in collection.FindAll())
        {
            SetIntIfMissing(settings, "MaxMemoryTokens", 400);
            SetIntIfMissing(settings, "SummarizationTriggerTokens", 1000);
            SetIntIfMissing(settings, "SummarizationDigestTokens", 500);
            SetIntIfMissing(settings, "SummaryMaxTokens", 200);
            SetIntIfMissing(settings, "MaxContextTokens", 1600);
            collection.Update(settings);
        }
    }

    private static void SetIntIfMissing(BsonDocument settings, string fieldName, int defaultValue)
    {
        settings[fieldName] = settings.ContainsKey(fieldName) && settings[fieldName].AsInt32 > 0 ? settings[fieldName].AsInt32 : defaultValue;
    }
}