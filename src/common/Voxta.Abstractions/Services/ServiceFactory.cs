using Microsoft.Extensions.DependencyInjection;
using Voxta.Abstractions.Exceptions;
using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Services;

public interface IServiceFactory<TInterface> where TInterface : class
{
    IEnumerable<string> ServiceNames { get; }
    Task<TInterface> CreateSpecificAsync(string service, string culture, bool dry, CancellationToken cancellationToken);
    Task<TInterface> CreateBestMatchAsync(ServicesList services, string preferredService, string[] prerequisites, string culture, CancellationToken cancellationToken);
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

    public async Task<TInterface> CreateSpecificAsync(string service, string culture, bool dry, CancellationToken cancellationToken)
    {
        if (!_registry.Types.TryGetValue(service, out var type))
            throw new NotSupportedException($"Service {service} is not registered");
        
        var instance = (TInterface)_sp.GetRequiredService(type);
        var success = await instance.TryInitializeAsync(Array.Empty<string>(), culture, dry, cancellationToken);
        if(!success) throw new ServiceDisabledException();
        return instance;
    }

    public async Task<TInterface> CreateBestMatchAsync(ServicesList services, string preferredService, string[] prerequisites, string culture, CancellationToken cancellationToken)
    {
        var options = string.IsNullOrEmpty(preferredService) ? services.Services : new[] { preferredService };
        foreach (var option in options)
        {
            if (!_registry.Types.TryGetValue(option, out var type))
                continue;  

            var instance = (TInterface)_sp.GetRequiredService(type);
            var success = await instance.TryInitializeAsync(prerequisites, culture, false, cancellationToken);
            if (success) return instance;
            if (instance.ServiceName == preferredService) throw new ServiceDisabledException();
        }

        throw new InvalidOperationException($"There is no {typeof(TInterface).Name} service compatible with features [{string.Join(", ", prerequisites)}] and culture {culture}");
    }
}