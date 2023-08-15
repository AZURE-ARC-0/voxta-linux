using LiteDB;
using Voxta.Abstractions.Model;

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
        UpdateCollection("OobaboogaSettings", "Oobabooga", true);
        UpdateCollection("TextGenerationWebUISettings", "TextGenerationWebUI", true);
        UpdateCollection("TextGenerationInferenceSettings", "TextGenerationInference", true);
        UpdateCollection("OpenAISettings", "OpenAI", true);
        UpdateCollection("KoboldAISettings", "KoboldAI", true);
        UpdateCollection("NovelAISettings", "NovelAI", true);
        
        UpdateCollection("AzureSpeechServiceSettings", "AzureSpeechService", false);
        UpdateCollection("ElevenLabsSettings", "ElevenLabs", false);
        UpdateCollection("FFmpegSettings", "FFmpeg", false);
        UpdateCollection("MockSettings", "Mock", false);
        UpdateCollection("VoskSettings", "Vosk", false);
        UpdateCollection("WindowsSpeechSettings", "WindowsSpeech", false);
    }

    private void UpdateCollection(string collectionName, string typeName, bool updateMemory)
    {
        var services = _db.GetCollection<ConfiguredService>();
        var collection = _db.GetCollection(collectionName);
        var first = true;
        foreach (var settings in collection.FindAll())
        {
            collection.Delete(settings);
            
            if (!first) continue;
            first = false;
            
            if (updateMemory)
            {
                SetIntIfMissing(settings, "MaxMemoryTokens", 400);
                SetIntIfMissing(settings, "SummarizationTriggerTokens", 1000);
                SetIntIfMissing(settings, "SummarizationDigestTokens", 500);
                SetIntIfMissing(settings, "SummaryMaxTokens", 200);
                SetIntIfMissing(settings, "MaxContextTokens", 1600);
            }

            var serviceRef = new ConfiguredService
            {
                Id = Guid.NewGuid(),
                Label = "",
                ServiceName = typeName
            };
            services.Insert(serviceRef);
            
            settings["_id"] = new BsonValue(serviceRef.Id);
            collection.Insert(settings);
        }
    }

    private static void SetIntIfMissing(BsonDocument settings, string fieldName, int defaultValue)
    {
        settings[fieldName] = settings.ContainsKey(fieldName) && settings[fieldName].AsInt32 > 0 ? settings[fieldName].AsInt32 : defaultValue;
    }
}