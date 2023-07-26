using Microsoft.Extensions.DependencyInjection;
using Voxta.Abstractions.Exceptions;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;

namespace Voxta.Abstractions.DependencyInjection;

public interface IServiceFactory<TInterface> where TInterface : class
{
    Task<TInterface> CreateAsync(ServicesList services, string preferredService, string[] prerequisites, string culture, CancellationToken cancellationToken);
}

public class ServiceFactory<TInterface> : IServiceFactory<TInterface> where TInterface : class, IService
{
    private readonly IServiceRegistry<TInterface> _registry;
    private readonly IServiceProvider _sp;

    public ServiceFactory(IServiceRegistry<TInterface> registry, IServiceProvider sp)
    {
        _registry = registry;
        _sp = sp;
    }

    public async Task<TInterface> CreateAsync(ServicesList services, string preferredService, string[] prerequisites, string culture, CancellationToken cancellationToken)
    {
        var options = string.IsNullOrEmpty(preferredService) ? services.Services : new[] { preferredService };
        foreach (var option in options)
        {
            if (!_registry.Types.TryGetValue(option, out var type))
                continue;

            var instance = (TInterface)_sp.GetRequiredService(type);
            var success = await instance.InitializeAsync(prerequisites, culture, cancellationToken);
            if (success) return instance;
            if (instance.ServiceName == preferredService) throw new ServiceDisabledException();
        }
        throw new InvalidOperationException($"There is no {typeof(TInterface).Name} service with name {preferredService}");
    }
}