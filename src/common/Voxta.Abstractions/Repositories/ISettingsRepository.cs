namespace Voxta.Abstractions.Repositories;

public interface ISettingsRepository
{
    Task<T?> GetAsync<T>(CancellationToken cancellationToken = default) where T : SettingsBase;
    Task SaveAsync<T>(T value) where T : SettingsBase;
    Task DeleteAsync<T>(T current) where T : SettingsBase;
}