using Voxta.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Voxta.Abstractions.DependencyInjection;

public interface IServiceRegistry<in TInterface> where TInterface : class
{
    void Add<TConcrete>(string key) where TConcrete : class, TInterface;
}

public class ServiceRegistry<TInterface> : IServiceRegistry<TInterface> where TInterface : class
{
    public readonly Dictionary<string, Type> Types = new();

    public void Add<TConcrete>(string key) where TConcrete : class, TInterface
    {
        if (string.IsNullOrEmpty(key) || key == "None") return;
        Types.Add(key, typeof(TConcrete));
    }
}

public interface IServiceFactory<TInterface> where TInterface : class
{
    Task<TInterface> CreateAsync(string serviceName, string[] prerequisites, string culture, CancellationToken cancellationToken);
}

public class ServiceFactory<TInterface> : IServiceFactory<TInterface> where TInterface : class, IService
{
    private readonly ServiceRegistry<TInterface> _registry;
    private readonly IServiceProvider _sp;

    public ServiceFactory(ServiceRegistry<TInterface> registry, IServiceProvider sp)
    {
        _registry = registry;
        _sp = sp;
    }

    public async Task<TInterface> CreateAsync(string serviceName, string[] prerequisites, string culture, CancellationToken cancellationToken)
    {
        #warning If the service is specified, ignore the prerequisites; if not, then go through the list and check the prerequisites.
        
        if (!_registry.Types.TryGetValue(serviceName, out var type))
            throw new InvalidOperationException($"There is no {typeof(TInterface).Name} service with name {serviceName}");
        
        var instance = (TInterface)_sp.GetRequiredService(type);
        await instance.InitializeAsync(prerequisites, culture, cancellationToken);
        
        return instance;
    }
}