using Voxta.Abstractions.Services;
using Voxta.Services.KoboldAI;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddKoboldAI(this IServiceCollection services)
    {
        services.AddTransient<KoboldAITextGenService>();
        services.AddTransient<KoboldAIActionInferenceService>();
    }
    
    public static void RegisterKoboldAI(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<KoboldAITextGenService>(KoboldAIConstants.ServiceName);
    }
    
    public static void RegisterKoboldAI(this IServiceRegistry<IActionInferenceService> registry)
    {
        registry.Add<KoboldAIActionInferenceService>(KoboldAIConstants.ServiceName);
    }
}