using Voxta.Abstractions.Services;
using Voxta.Services.TextGenerationInference;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddTextGenerationInference(this IServiceCollection services)
    {
        services.AddTransient<TextGenerationInferenceTextGenService>();
        services.AddTransient<TextGenerationInferenceActionInferenceService>();
    }
    
    public static void RegisterTextGenerationInference(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<TextGenerationInferenceTextGenService>(TextGenerationInferenceConstants.ServiceName);
    }
    
    public static void RegisterTextGenerationInference(this IServiceRegistry<IActionInferenceService> registry)
    {
        registry.Add<TextGenerationInferenceActionInferenceService>(TextGenerationInferenceConstants.ServiceName);
    }
}