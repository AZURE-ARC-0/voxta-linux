using Voxta.Abstractions.Model;
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
    
    public static void RegisterTextGenerationInference(this IServiceDefinitionsRegistry registry)
    {
        registry.Add(new ServiceDefinition
        {
            ServiceName = TextGenerationInferenceConstants.ServiceName,
            Label = "HuggingFace Text Generation Inference",
            TextGen = ServiceDefinitionCategoryScore.Medium,
            STT = ServiceDefinitionCategoryScore.NotSupported,
            TTS = ServiceDefinitionCategoryScore.NotSupported,
            Summarization = ServiceDefinitionCategoryScore.Medium,
            ActionInference = ServiceDefinitionCategoryScore.Medium,
            Features = new[] { ServiceFeatures.NSFW },
            Recommended = false,
            Notes = "HuggingFace's open source local large language models host. Did not gave great results in our tests, but maybe our implementation is wrong.",
            SettingsType = typeof(TextGenerationInferenceSettings),
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