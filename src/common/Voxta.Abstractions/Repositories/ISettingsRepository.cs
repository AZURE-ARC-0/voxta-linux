using LiteDB;

namespace Voxta.Abstractions.Repositories;

public interface ISettingsRepository
{
    Task<T?> GetAsync<T>(CancellationToken cancellationToken = default) where T : SettingsBase;
    Task SaveAsync<T>(T value) where T : SettingsBase;
}

[Serializable]
public abstract class SettingsBase
{
    public static readonly string SharedId = Guid.Empty.ToString();
    
    [BsonId] public string Id { get; init; } = SharedId;
}