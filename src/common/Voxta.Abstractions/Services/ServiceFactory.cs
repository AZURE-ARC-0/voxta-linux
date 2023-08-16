using Microsoft.Extensions.DependencyInjection;
using Voxta.Abstractions.Exceptions;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;

namespace Voxta.Abstractions.Services;

public interface IServiceFactory<TInterface> where TInterface : class
{
    IEnumerable<string> ServiceNames { get; }
    Task<TInterface> CreateSpecificAsync(Guid serviceId, string culture, bool dry, CancellationToken cancellationToken);
    Task<TInterface> CreateBestMatchAsync(ServicesList services, Guid? preferredServiceId, string[] prerequisites, string culture, CancellationToken cancellationToken);
}

public class ServiceFactory<TInterface> : IServiceFactory<TInterface> where TInterface : class, IService
{
    private readonly IServiceRegistry<TInterface> _registry;
    private readonly IServiceProvider _sp;

    public IEnumerable<string> ServiceNames => _registry.Types.Keys;

    public ServiceFactory(IServiceRegistry<TInterface> registry, IServiceProvider sp)
    {
        _registry = registry;
        _sp = sp;
    }

    public async Task<TInterface> CreateSpecificAsync(Guid serviceId, string culture, bool dry, CancellationToken cancellationToken)
    {
        var servicesRepository = _sp.GetRequiredService<IServicesRepository>();
        var serviceRef = await servicesRepository.GetServiceAsync(serviceId, cancellationToken);
        if (serviceRef == null) throw new NullReferenceException($"Could not find a service with ID {serviceId}");
        if (!_registry.Types.TryGetValue(serviceRef.ServiceName, out var type))
            throw new NotSupportedException($"Service {serviceRef.ServiceName} referenced by service ID {serviceId} is not registered");
        
        var instance = (TInterface)_sp.GetRequiredService(type);
        var success = await instance.TryInitializeAsync(serviceId, Array.Empty<string>(), culture, dry, cancellationToken);
        if(!success) throw new ServiceDisabledException();
        return instance;
    }

    public async Task<TInterface> CreateBestMatchAsync(ServicesList services, Guid? preferredServiceId, string[] prerequisites, string culture, CancellationToken cancellationToken)
    {
        if (preferredServiceId != null)
            return await CreateSpecificAsync(preferredServiceId.Value, culture, false, cancellationToken);

        if (services.Services.Length == 0)
            throw new InvalidOperationException($"There is not {typeof(TInterface).Name} service configured");
        
        foreach (var serviceId in services.Services)
        {
            var servicesRepository = _sp.GetRequiredService<IServicesRepository>();
            var serviceRef = await servicesRepository.GetServiceAsync(serviceId, cancellationToken);
            if (serviceRef == null) continue;
            if (!serviceRef.Enabled) continue;
            
            if (!_registry.Types.TryGetValue(serviceRef.ServiceName, out var type))
                continue;  

            var instance = (TInterface)_sp.GetRequiredService(type);
            var success = await instance.TryInitializeAsync(serviceId, prerequisites, culture, false, cancellationToken);
            if (success) return instance;
        }

        throw new InvalidOperationException($"There is no {typeof(TInterface).Name} service compatible with features [{(prerequisites.Length > 0 ? string.Join(", ", prerequisites) : "(none)")}] and culture {culture}");
    }
}