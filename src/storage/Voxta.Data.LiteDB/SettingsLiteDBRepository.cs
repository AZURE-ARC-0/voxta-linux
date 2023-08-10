using Voxta.Abstractions.Repositories;
using LiteDB;

namespace Voxta.Data.LiteDB;

public class SettingsLiteDBRepository : ISettingsRepository
{
    private readonly ILiteDatabase _db;

    public SettingsLiteDBRepository(ILiteDatabase db)
    {
        _db = db;
    }

    public Task<T?> GetAsync<T>(CancellationToken cancellationToken = default) where T : SettingsBase
    {
        var collection = _db.GetCollection<T>();
        var settings = collection.FindOne(x => x.Id == SettingsBase.SharedId);
        return Task.FromResult<T?>(settings);
    }

    public Task SaveAsync<T>(T value) where T : SettingsBase
    {
        var collection = _db.GetCollection<T>();
        collection.Upsert(value);
        return Task.CompletedTask;
    }

    public Task DeleteAsync<T>(T current) where T : SettingsBase
    {
        var collection = _db.GetCollection<T>();
        collection.Delete(current.Id);
        return Task.CompletedTask;
    }
}