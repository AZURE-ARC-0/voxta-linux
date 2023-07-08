using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Services;
using ChatMate.Services.KoboldAI;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKoboldAI(this IServiceCollection services)
    {
        services.AddScoped<KoboldAITextGenClient>();
        return services;
    }
    
    public static void RegisterKoboldAI(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<KoboldAITextGenClient>(KoboldAIConstants.ServiceName);
    }
}