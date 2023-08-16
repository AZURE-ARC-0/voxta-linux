using Voxta.Abstractions.Repositories;

namespace Voxta.Abstractions.Services;

public abstract class ServiceBase<TSettings> where TSettings : SettingsBase
{
    public abstract string ServiceName { get; }
    
    private ServiceSettingsRef? _settingsRef;
    public ServiceSettingsRef SettingsRef => _settingsRef ?? throw new NullReferenceException("Service not initialized");

    private readonly ISettingsRepository _settingsRepository;

    public ServiceBase(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<bool> TryInitializeAsync(Guid serviceId, string[] prerequisites, string culture, bool dry, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetRequiredAsync<TSettings>(serviceId, cancellationToken);
        _settingsRef = new ServiceSettingsRef
        {
            ServiceName = ServiceName,
            ServiceId = serviceId
        };
        return await TryInitializeAsync(settings, prerequisites, culture, dry, cancellationToken);
    }

    protected virtual Task<bool> TryInitializeAsync(TSettings settings, string[] prerequisites, string culture, bool dry, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    protected void PreInitializeAsync(Guid serviceId)
    {
        _settingsRef = new ServiceSettingsRef { ServiceName = ServiceName, ServiceId = serviceId };
    }
}