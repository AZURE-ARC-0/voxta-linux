﻿#if(WINDOWS)
using Voxta.Abstractions.Services;
using Voxta.Services.WindowsSpeech;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddWindowsSpeech(this IServiceCollection services)
    {
        services.AddTextToSpeechUtils();
        services.AddTransient<WindowsSpeechTextToSpeechService>();
        services.AddTransient<WindowsSpeechSpeechToText>();
    }
    
    public static void RegisterWindowsSpeech(this IServiceDefinitionsRegistry registry)
    {
        registry.Add(new ServiceDefinition
        {
            ServiceName = WindowsSpeechConstants.ServiceName,
            Label = "Windows Speech",
            TextGen = ServiceDefinitionCategoryScore.NotSupported,
            STT = ServiceDefinitionCategoryScore.Low,
            TTS = ServiceDefinitionCategoryScore.Low,
            Summarization = ServiceDefinitionCategoryScore.NotSupported,
            ActionInference = ServiceDefinitionCategoryScore.NotSupported,
            Features = Array.Empty<string>(),
            Recommended = false,
            Notes = "Fair quality speech transcription and synthesizer. Supports your installed languages. Censored.",
            SettingsType = typeof(WindowsSpeechSettings),
        });
    }
    
    public static void RegisterWindowsSpeech(this IServiceRegistry<ITextToSpeechService> registry)
    {
        registry.Add<WindowsSpeechTextToSpeechService>(WindowsSpeechConstants.ServiceName);
    }
    
    public static void RegisterWindowsSpeech(this IServiceRegistry<ISpeechToTextService> registry)
    {
        registry.Add<WindowsSpeechSpeechToText>(WindowsSpeechConstants.ServiceName);
    }
}
#endif