using ChatMate.Abstractions.Repositories;
using LiteDB;

namespace ChatMate.Data.LiteDB;

public class SettingsLiteDBRepository : ISettingsRepository
{
    private readonly ILiteDatabase _db;

    public SettingsLiteDBRepository(ILiteDatabase db)
    {
        _db = db;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class, ISettings
    {
        var collection = _db.GetCollection<T>();
        var settings = collection.FindOne(x => true);
        return Task.FromResult<T?>(settings);
    }

    public Task SaveAsync<T>(string key, T value) where T : class, ISettings
    {
        #warning Remove key?
        var collection = _db.GetCollection<T>();
        collection.Upsert(value);
        return Task.CompletedTask;
    }
}