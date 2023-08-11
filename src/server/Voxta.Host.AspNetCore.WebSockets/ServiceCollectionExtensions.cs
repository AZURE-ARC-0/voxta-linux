using Voxta.Abstractions.Diagnostics;
using Microsoft.AspNetCore.WebSockets;
using Voxta.Abstractions.Management;
using Voxta.Abstractions.Services;
using Voxta.Abstractions.System;
using Voxta.Core;
using Voxta.Host.AspNetCore.WebSockets;
using Voxta.Host.AspNetCore.WebSockets.Utils;
using Voxta.Server.BackgroundServices;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddVoxtaServer(this IServiceCollection services)
    {
        services.AddWebSockets(_ => { });
        
        services.AddHttpClient();
        services.AddNAudio();
        services.AddVoxta();
        services.AddSingleton<IPerformanceMetrics, StaticPerformanceMetrics>();
        services.AddLiteDBRepositories();
        services.AddTransient<DiagnosticsUtil>();
        services.AddSingleton<ILocalEncryptionProvider, DPAPIEncryptionProvider>();
        services.AddSingleton<IServiceObserver, StaticServiceObserver>();
        services.AddTransient<IMemoryProvider, SimpleMemoryProvider>();
        
        services.AddSingleton<TemporaryFileCleanupService>();
        services.AddSingleton<ITemporaryFileCleanup>(sp => sp.GetRequiredService<TemporaryFileCleanupService>());
        services.AddHostedService(sp => sp.GetRequiredService<TemporaryFileCleanupService>());

        AddAllServices(services);
    }

    private static void AddAllServices(IServiceCollection services)
    {
        var speechToTextRegistry = services.AddSpeechToTextRegistry();
        var textGenRegistry = services.AddTextGenRegistry();
        var textToSpeechRegistry = services.AddTextToSpeechRegistry();
        var actionInferenceRegistry = services.AddActionInferenceRegistry();
        var summarizationRegistry = services.AddSummarizationRegistry();

        services.AddMocks();
        textGenRegistry.RegisterMocks();
        textToSpeechRegistry.RegisterMocks();
        actionInferenceRegistry.RegisterMocks();
        summarizationRegistry.RegisterMocks();

        services.AddOpenAI();
        textGenRegistry.RegisterOpenAI();
        actionInferenceRegistry.RegisterOpenAI();

        services.AddNovelAI();
        textGenRegistry.RegisterNovelAI();
        textToSpeechRegistry.RegisterNovelAI();
        actionInferenceRegistry.RegisterNovelAI();

        services.AddKoboldAI();
        textGenRegistry.RegisterKoboldAI();
        actionInferenceRegistry.RegisterKoboldAI();
        summarizationRegistry.RegisterKoboldAI();
        
        services.AddOobabooga();
        textGenRegistry.RegisterOobabooga();
        actionInferenceRegistry.RegisterOobabooga();
        summarizationRegistry.RegisterOobabooga();
        
        services.AddTextGenerationInference();
        textGenRegistry.RegisterTextGenerationInference();
        actionInferenceRegistry.RegisterTextGenerationInference();

        services.AddElevenLabs();
        textToSpeechRegistry.RegisterElevenLabs();

        services.AddVosk();
        speechToTextRegistry.RegisterVosk();

        services.AddAzureSpeechService();
        textToSpeechRegistry.RegisterAzureSpeechService();
        speechToTextRegistry.RegisterAzureSpeechService();

        #if(WINDOWS)
        
        services.AddWindowsSpeech();
        textToSpeechRegistry.RegisterWindowsSpeech();
        speechToTextRegistry.RegisterWindowsSpeech();
        
        #endif
        
        #if(LINUX)
        
        services.AddFFmpeg();
        speechToTextRegistry.RegisterFFmpeg();
        
        #endif
    }
}