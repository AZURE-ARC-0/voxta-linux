using Microsoft.Extensions.DependencyInjection;
using Voxta.Abstractions.Exceptions;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;

namespace Voxta.Abstractions.Services;

public interface IServiceFactory<TInterface> where TInterface : class
{
    IEnumerable<string> ServiceNames { get; }
    Task<TInterface> CreateSpecificAsync(ServiceLink link, string culture, bool dry, CancellationToken cancellationToken);
    Task<TInterface> CreateBestMatchAsync(ServicesList services, ServiceLink? preferred, string[] prerequisites, string culture, CancellationToken cancellationToken);
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

    public async Task<TInterface> CreateSpecificAsync(ServiceLink link, string culture, bool dry, CancellationToken cancellationToken)
    {
        var servicesRepository = _sp.GetRequiredService<IServicesRepository>();
        var serviceRef = link.ServiceId != null
            ? await servicesRepository.GetServiceByIdAsync(link.ServiceId.Value, cancellationToken)
            : await servicesRepository.GetServiceByNameAsync(link.ServiceName, cancellationToken);
        if (serviceRef == null) throw new NullReferenceException($"Could not find a service {link}");
        if (!_registry.Types.TryGetValue(serviceRef.ServiceName, out var type))
            throw new NotSupportedException($"Service {serviceRef.ServiceName} referenced by service {link} is not registered");
        
        var instance = (TInterface)_sp.GetRequiredService(type);
        var success = await instance.TryInitializeAsync(serviceRef.Id, Array.Empty<string>(), culture, dry, cancellationToken);
        if(!success) throw new ServiceDisabledException();
        return instance;
    }

    public async Task<TInterface> CreateBestMatchAsync(ServicesList services, ServiceLink? preferred, string[] prerequisites, string culture, CancellationToken cancellationToken)
    {
        var servicesRepository = _sp.GetRequiredService<IServicesRepository>();

        if (preferred != null)
        {
            var service = await TryCreateOneAsync(preferred, prerequisites, culture, servicesRepository, cancellationToken);
            if (service == null) throw new InvalidOperationException($"Preferred service with name {preferred.ServiceName} and ID {(preferred.ServiceId?.ToString() ?? "Any")} was either not found or was not compatible with features [{(prerequisites.Length > 0 ? string.Join(", ", prerequisites) : "(none)")}] and culture {culture}");
            return service;
        }

        if (services.Services.Length == 0)
            throw new InvalidOperationException($"There is not {typeof(TInterface).Name} service configured");
        
        foreach (var serviceLink in services.Services)
        {
            var service = await TryCreateOneAsync(serviceLink, prerequisites, culture, servicesRepository, cancellationToken);
            if (service != null) return service;
        }

        throw new InvalidOperationException($"There is no {typeof(TInterface).Name} service compatible with features [{(prerequisites.Length > 0 ? string.Join(", ", prerequisites) : "(none)")}] and culture {culture}");
    }

    private async Task<TInterface?> TryCreateOneAsync(ServiceLink serviceLink, string[] prerequisites, string culture, IServicesRepository servicesRepository, CancellationToken cancellationToken)
    {
        var serviceRef = serviceLink.ServiceId != null ? await servicesRepository.GetServiceByIdAsync(serviceLink.ServiceId.Value, cancellationToken) : null;
        if (serviceRef == null) serviceRef = await servicesRepository.GetServiceByNameAsync(serviceLink.ServiceName, cancellationToken);
        if (serviceRef == null) return null;
        if (!serviceRef.Enabled) return null;

        if (!_registry.Types.TryGetValue(serviceRef.ServiceName, out var type))
            return null;

        var instance = (TInterface)_sp.GetRequiredService(type);
        var success = await instance.TryInitializeAsync(serviceRef.Id, prerequisites, culture, false, cancellationToken);
        return success ? instance : null;
    }
}