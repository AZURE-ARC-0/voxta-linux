using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Repositories;

public interface ISettingsRepository
{
    Task<T?> GetAsync<T>(Guid serviceId, CancellationToken cancellationToken = default) where T : SettingsBase;
}

public static class SettingsRepositoryExtensions
{
    public static async Task<T> GetRequiredAsync<T>(this ISettingsRepository settingsRepository, Guid serviceId, CancellationToken cancellationToken = default)
        where T : SettingsBase
    {
        var settings = await settingsRepository.GetAsync<T>(serviceId, cancellationToken);
        if (settings == null) throw new NullReferenceException($"Could not find service with ID {serviceId}");
        return settings;
    }
}