using Voxta.Abstractions.Repositories;
using LiteDB;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;

namespace Voxta.Data.LiteDB;

public class ServicesLiteDBRepository : IServicesRepository
{
    private readonly ILiteDatabase _db;
    private readonly IServiceDefinitionsRegistry _serviceDefinitionsRegistry;
    private readonly ILiteCollection<ConfiguredService> _servicesCollection;

    public ServicesLiteDBRepository(ILiteDatabase db, IServiceDefinitionsRegistry serviceDefinitionsRegistry)
    {
        _db = db;
        _serviceDefinitionsRegistry = serviceDefinitionsRegistry;
        _servicesCollection = db.GetCollection<ConfiguredService>();
    }

    public Task<ConfiguredService[]> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        var services = _servicesCollection.FindAll().ToArray();
        return Task.FromResult(services);
    }

    public Task<ConfiguredService?> GetServiceByIdAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var services = _servicesCollection.FindOne(x => x.Id == serviceId);
        return Task.FromResult<ConfiguredService?>(services);
    }

    public Task<ConfiguredService?> GetServiceByNameAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var services = _servicesCollection.FindOne(x => x.ServiceName == serviceName && x.Enabled);
        return Task.FromResult<ConfiguredService?>(services);
    }

    public Task<ConfiguredService<T>?> GetAsync<T>(Guid serviceId, CancellationToken cancellationToken = default) where T : SettingsBase
    {
        var collection = _db.GetCollection<T>();
        var service = _servicesCollection.FindOne(x => x.Id == serviceId);
        if (service == null) return Task.FromResult<ConfiguredService<T>?>(null);
        var settings = collection.FindOne(x => x.Id == serviceId);
        if (settings == null) return Task.FromResult<ConfiguredService<T>?>(null);
        return Task.FromResult<ConfiguredService<T>?>(new ConfiguredService<T>
        {
            Id = serviceId,
            Label = service.Label,
            Enabled = service.Enabled,
            ServiceName = service.ServiceName,
            Settings = settings,
        });
    }

    public Task SaveAsync<T>(ConfiguredService<T> configuredService) where T : SettingsBase
    {
        if (configuredService.Id == Guid.Empty) throw new InvalidOperationException("Id must be set");
        configuredService.Settings.Id = configuredService.Id;
        _servicesCollection.Upsert(new ConfiguredService
        {
            Id = configuredService.Id,
            Label = configuredService.Label,
            ServiceName = configuredService.ServiceName,
            Enabled = configuredService.Enabled,
        });
        var settings = configuredService.Settings;
        var collection = _db.GetCollection<T>();
        collection.Upsert(settings);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid serviceId)
    {
        var service = _servicesCollection.FindOne(x => x.Id == serviceId);
        if (service == null) return Task.CompletedTask;
        var settingsType = _serviceDefinitionsRegistry.Get(service.ServiceName)?.SettingsType;
        if (settingsType == null) return Task.CompletedTask;
        var collection = _db.GetCollection(settingsType.Name);
        _servicesCollection.Delete(serviceId);
        collection.Delete(serviceId);
        return Task.CompletedTask;
    }
}
