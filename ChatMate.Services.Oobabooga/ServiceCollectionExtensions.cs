using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Services;
using ChatMate.Services.Oobabooga;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddOobabooga(this IServiceCollection services)
    {
        services.AddScoped<OobaboogaTextGenClient>();
    }
    
    public static void RegisterOobabooga(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<OobaboogaTextGenClient>(OobaboogaConstants.ServiceName);
    }
}