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

    public Task<T?> GetAsync<T>(Guid serviceId, CancellationToken cancellationToken = default) where T : SettingsBase
    {
        var collection = _db.GetCollection<T>();
        var settings = collection.FindOne(x => x.Id == serviceId);
        return Task.FromResult<T?>(settings);
    }
}