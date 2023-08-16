using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Repositories;

public interface IServicesRepository
{
    Task<ConfiguredService[]> GetServicesAsync(CancellationToken cancellationToken = default);
    Task<ConfiguredService?> GetServiceAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<ConfiguredService<T>?> GetAsync<T>(Guid serviceId, CancellationToken cancellationToken = default) where T : SettingsBase;
    Task SaveAsync<T>(ConfiguredService<T> configuredService) where T : SettingsBase;
    Task DeleteAsync(Guid serviceId);
}
