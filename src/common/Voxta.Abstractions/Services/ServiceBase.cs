using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;

namespace Voxta.Abstractions.Services;

public abstract class ServiceBase<TSettings> where TSettings : SettingsBase
{
    protected abstract string ServiceName { get; }
    
    private ServiceSettingsRef? _settingsRef;
    public ServiceSettingsRef SettingsRef => _settingsRef ?? throw new NullReferenceException("Service not initialized");

    private readonly ISettingsRepository _settingsRepository;

    protected ServiceBase(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<bool> TryInitializeAsync(Guid serviceId, IPrerequisitesValidator prerequisites, string culture, bool dry, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetRequiredAsync<TSettings>(serviceId, cancellationToken);
        _settingsRef = new ServiceSettingsRef
        {
            ServiceName = ServiceName,
            ServiceId = serviceId
        };
        return await TryInitializeAsync(settings, prerequisites, culture, dry, cancellationToken);
    }

    protected virtual Task<bool> TryInitializeAsync(TSettings settings, IPrerequisitesValidator prerequisites, string culture, bool dry, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    protected void PreInitializeAsync(Guid serviceId)
    {
        _settingsRef = new ServiceSettingsRef { ServiceName = ServiceName, ServiceId = serviceId };
    }
}