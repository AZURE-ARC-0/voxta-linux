using ChatMate.Abstractions.Model;
using LiteDB;

namespace ChatMate.Data.LiteDB;

public class LiteDBMigrations
{
    private readonly ILiteDatabase _db;

    public LiteDBMigrations(ILiteDatabase db)
    {
        _db = db;
    }
    
    public Task MigrateAsync()
    {
        if (_db.UserVersion == 0)
        {
            Migrate_0_To_1();
            _db.UserVersion = 1;
        }

        return Task.CompletedTask;
    }

    private void Migrate_0_To_1()
    {
        var profileSettings = _db.GetCollection("ProfileSettings");
        foreach (var doc in profileSettings.FindAll())
        {
            // Remove obsolete items
            if (doc["_id"].Type == BsonType.ObjectId) profileSettings.Delete(doc["_id"]);
            // Update field name
            var animationSelectionService = doc["AnimationSelectionService"];
            doc.Remove("AnimationSelectionService");
            doc["Services"] = BsonMapper.Global.Serialize(new ProfileSettings.ProfileServicesMap
            {
                SpeechToText = new SpeechToTextServiceMap
                {
                    Service = "Vosk",
                    Model = "vosk-model-small-en-us-0.15",
                    Hash = "30f26242c4eb449f948e42cb302dd7a686cb29a3423a8367f99ff41780942498"
                },
                ActionInference = new ServiceMap
                {
                    Service = animationSelectionService ?? "OpenAI"
                }
            });
            profileSettings.Update(doc);
        }
    }
}