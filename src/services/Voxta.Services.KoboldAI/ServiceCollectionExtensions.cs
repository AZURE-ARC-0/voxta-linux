using Voxta.Abstractions.DependencyInjection;
using Voxta.Abstractions.Services;
using Voxta.Services.KoboldAI;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddKoboldAI(this IServiceCollection services)
    {
        services.AddTransient<KoboldAITextGenClient>();
    }
    
    public static void RegisterKoboldAI(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<KoboldAITextGenClient>(KoboldAIConstants.ServiceName);
    }
}