using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;
using Voxta.Services.AzureSpeechService;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddAzureSpeechService(this IServiceCollection services)
    {
        services.AddTransient<AzureSpeechServiceSpeechToText>();
        services.AddTransient<AzureSpeechServiceTextToSpeech>();
    }
    
    public static void RegisterAzureSpeechService(this IServiceDefinitionsRegistry registry)
    {
        registry.Add(new ServiceDefinition
        {
            ServiceName = AzureSpeechServiceConstants.ServiceName,
            Label = "Azure Speech Service",
            TextGen = ServiceDefinitionCategoryScore.NotSupported,
            STT = ServiceDefinitionCategoryScore.High,
            TTS = ServiceDefinitionCategoryScore.Medium,
            Summarization = ServiceDefinitionCategoryScore.NotSupported,
            ActionInference = ServiceDefinitionCategoryScore.NotSupported,
            Features = new[] { ServiceFeatures.NSFW },
            SettingsType = typeof(AzureSpeechServiceSettings),
        });
    }
    
    public static void RegisterAzureSpeechService(this IServiceRegistry<ITextToSpeechService> registry)
    {
        registry.Add<AzureSpeechServiceTextToSpeech>(AzureSpeechServiceConstants.ServiceName);
    }
    
    public static void RegisterAzureSpeechService(this IServiceRegistry<ISpeechToTextService> registry)
    {
        registry.Add<AzureSpeechServiceSpeechToText>(AzureSpeechServiceConstants.ServiceName);
    }
}