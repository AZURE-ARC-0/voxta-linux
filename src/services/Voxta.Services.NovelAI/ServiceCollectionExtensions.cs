﻿using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;
using Voxta.Services.NovelAI;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddNovelAI(this IServiceCollection services)
    {
        services.AddTextToSpeechUtils();
        services.AddTransient<NovelAITextGenService>();
        services.AddTransient<NovelAITextToSpeechService>();
        services.AddTransient<NovelAIActionInferenceService>();
        services.AddTransient<NovelAISummarizationService>();
    }
    
    public static void RegisterNovelAI(this IServiceDefinitionsRegistry registry)
    {
        registry.Add(new ServiceDefinition
        {
            ServiceName = NovelAIConstants.ServiceName,
            Label = "NovelAI",
            TextGen = ServiceDefinitionCategoryScore.Medium,
            STT = ServiceDefinitionCategoryScore.NotSupported,
            TTS = ServiceDefinitionCategoryScore.High,
            Summarization = ServiceDefinitionCategoryScore.Medium,
            ActionInference = ServiceDefinitionCategoryScore.Medium,
            Features = new[] { ServiceFeatures.NSFW },
            Recommended = true,
            Notes = "Amazing text to speech and large language model. Paid. Supports English and Japanese.",
            SettingsType = typeof(NovelAISettings),
        });
    }
    
    public static void RegisterNovelAI(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<NovelAITextGenService>(NovelAIConstants.ServiceName);
    }
    
    public static void RegisterNovelAI(this IServiceRegistry<ITextToSpeechService> registry)
    {
        registry.Add<NovelAITextToSpeechService>(NovelAIConstants.ServiceName);
    }
    
    public static void RegisterNovelAI(this IServiceRegistry<IActionInferenceService> registry)
    {
        registry.Add<NovelAIActionInferenceService>(NovelAIConstants.ServiceName);
    }
    
    public static void RegisterNovelAI(this IServiceRegistry<ISummarizationService> registry)
    {
        registry.Add<NovelAISummarizationService>(NovelAIConstants.ServiceName);
    }
}