﻿using Voxta.Abstractions.Diagnostics;
using Voxta.Core;
using Voxta.Data.LiteDB;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Voxta.Abstractions.Management;
using Voxta.Host.AspNetCore.WebSockets.Utils;
using Voxta.Server.BackgroundServices;

namespace Voxta.Host.AspNetCore.WebSockets;

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

        services.AddMocks();
        textGenRegistry.RegisterMocks();
        textToSpeechRegistry.RegisterMocks();
        actionInferenceRegistry.RegisterMocks();

        services.AddOpenAI();
        textGenRegistry.RegisterOpenAI();
        actionInferenceRegistry.RegisterOpenAI();

        services.AddNovelAI();
        textGenRegistry.RegisterNovelAI();
        textToSpeechRegistry.RegisterNovelAI();

        services.AddKoboldAI();
        textGenRegistry.RegisterKoboldAI();
        actionInferenceRegistry.RegisterKoboldAI();
        
        services.AddOobabooga();
        textGenRegistry.RegisterOobabooga();
        actionInferenceRegistry.RegisterOobabooga();

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
    }
}