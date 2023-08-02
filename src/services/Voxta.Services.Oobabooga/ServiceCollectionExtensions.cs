using Voxta.Abstractions.Services;
using Voxta.Services.Oobabooga;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddOobabooga(this IServiceCollection services)
    {
        services.AddTransient<OobaboogaTextGenService>();
        services.AddTransient<OobaboogaActionInferenceService>();
    }
    
    public static void RegisterOobabooga(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<OobaboogaTextGenService>(OobaboogaConstants.ServiceName);
    }
    
    public static void RegisterOobabooga(this IServiceRegistry<IActionInferenceService> registry)
    {
        registry.Add<OobaboogaActionInferenceService>(OobaboogaConstants.ServiceName);
    }
}