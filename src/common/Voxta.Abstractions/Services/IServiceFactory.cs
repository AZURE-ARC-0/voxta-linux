using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Services;

public interface IServiceFactory<TInterface> where TInterface : class
{
    Task<TInterface> CreateSpecificAsync(ServiceLink link, string culture, bool dry, CancellationToken cancellationToken);
    Task<TInterface?> CreateBestMatchAsync(ServicesList services, ServiceLink? preferred, IPrerequisitesValidator prerequisites, string culture, CancellationToken cancellationToken);
}

public static class ServiceFactoryExtensions
{
    public static async Task<TInterface> CreateBestMatchRequiredAsync<TInterface>(this IServiceFactory<TInterface> factory, ServicesList services, ServiceLink? preferred,
        IPrerequisitesValidator prerequisites, string culture, CancellationToken cancellationToken)
        where TInterface : class, IService
    {
        var service = await factory.CreateBestMatchAsync(services, preferred, prerequisites, culture, cancellationToken);
        if (service != null) return service;
        throw new InvalidOperationException($"There is no {typeof(TInterface).Name} service compatible with prerequisites {prerequisites}");
    }
}
