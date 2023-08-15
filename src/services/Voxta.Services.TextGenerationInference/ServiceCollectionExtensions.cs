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
        services.AddTransient<TextGenerationInferenceSummarizationService>();
    }
    
    public static void RegisterTextGenerationInference(this IServiceHelpRegistry registry)
    {
        registry.Add(new ServiceHelp
        {
            ServiceName = TextGenerationInferenceConstants.ServiceName,
            Label = "HuggingFace Text Generation Inference",
            TextGen = true,
            STT = false,
            TTS = false,
            Summarization = true,
            ActionInference = true,
        });
    }
    
    public static void RegisterTextGenerationInference(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<TextGenerationInferenceTextGenService>(TextGenerationInferenceConstants.ServiceName);
    }
    
    public static void RegisterTextGenerationInference(this IServiceRegistry<IActionInferenceService> registry)
    {
        registry.Add<TextGenerationInferenceActionInferenceService>(TextGenerationInferenceConstants.ServiceName);
    }
    
    public static void RegisterTextGenerationInference(this IServiceRegistry<ISummarizationService> registry)
    {
        registry.Add<TextGenerationInferenceSummarizationService>(TextGenerationInferenceConstants.ServiceName);
    }
}