using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;
using Voxta.Services.ElevenLabs;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddElevenLabs(this IServiceCollection services)
    {
        services.AddTransient<ElevenLabsTextToSpeechService>();
    }
    
    public static void RegisterElevenLabs(this IServiceDefinitionsRegistry registry)
    {
        registry.Add(new ServiceDefinition
        {
            ServiceName = ElevenLabsConstants.ServiceName,
            Label = "11ElevenLabs",
            TextGen = ServiceDefinitionCategoryScore.NotSupported,
            STT = ServiceDefinitionCategoryScore.NotSupported,
            TTS = ServiceDefinitionCategoryScore.High,
            Summarization = ServiceDefinitionCategoryScore.NotSupported,
            ActionInference = ServiceDefinitionCategoryScore.NotSupported,
            Features = new[] { ServiceFeatures.NSFW },
            SettingsType = typeof(ElevenLabsSettings),
        });
    }
    
    public static void RegisterElevenLabs(this IServiceRegistry<ITextToSpeechService> registry)
    {
        registry.Add<ElevenLabsTextToSpeechService>(ElevenLabsConstants.ServiceName);
    }
}