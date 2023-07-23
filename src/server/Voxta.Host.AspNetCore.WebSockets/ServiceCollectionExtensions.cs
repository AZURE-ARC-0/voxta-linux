using ChatMate.Abstractions.Diagnostics;
using ChatMate.Core;
using ChatMate.Data.LiteDB;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.DependencyInjection;

namespace ChatMate.Host.AspNetCore.WebSockets;

public static class ServiceCollectionExtensions
{
    public static void AddChatMateServer(this IServiceCollection services)
    {
        services.AddWebSockets(_ => { });
        
        services.AddHttpClient();
        services.AddNAudio();
        services.AddChatMate();
        services.AddSingleton<IPerformanceMetrics, StaticPerformanceMetrics>();
        services.AddLiteDBRepositories();

        var speechToTextRegistry = services.AddSpeechToTextRegistry();
        var textGenRegistry = services.AddTextGenRegistry();
        var textToSpeechRegistry = services.AddTextToSpeechRegistry();
        var actionInferenceRegistry = services.AddActionInferenceRegistry();

        services.AddFakes();
        textGenRegistry.RegisterFakes();
        textToSpeechRegistry.RegisterFakes();
        actionInferenceRegistry.RegisterFakes();
        
        services.AddOpenAI();
        textGenRegistry.RegisterOpenAI();
        actionInferenceRegistry.RegisterOpenAI();

        services.AddNovelAI();
        textGenRegistry.RegisterNovelAI();
        textToSpeechRegistry.RegisterNovelAI();
        
        services.AddKoboldAI();
        textGenRegistry.RegisterKoboldAI();
        
        services.AddOobabooga();
        textGenRegistry.RegisterOobabooga();
        actionInferenceRegistry.RegisterOobabooga();
        
        services.AddElevenLabs();
        textToSpeechRegistry.RegisterElevenLabs();

        services.AddVosk();
        speechToTextRegistry.RegisterVosk();
    }
}