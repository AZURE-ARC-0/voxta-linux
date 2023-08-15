namespace Voxta.Abstractions.Repositories;

public interface ISettingsRepository
{
    Task<T?> GetAsync<T>(Guid serviceId, CancellationToken cancellationToken = default) where T : SettingsBase;
}
