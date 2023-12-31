﻿using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Repositories;

public interface IServicesRepository
{
    Task<ConfiguredService[]> GetServicesAsync(CancellationToken cancellationToken = default);
    Task<ConfiguredService?> GetServiceByIdAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<ConfiguredService?> GetServiceByNameAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<ConfiguredService<T>?> GetAsync<T>(Guid serviceId, CancellationToken cancellationToken = default) where T : SettingsBase;
    Task SaveServiceAsync(ConfiguredService service);
    Task SaveServiceAndSettingsAsync<T>(ConfiguredService<T> configuredService) where T : SettingsBase;
    Task DeleteAsync(Guid serviceId);
}
