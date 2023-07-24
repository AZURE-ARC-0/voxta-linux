using Voxta.Abstractions.Diagnostics;
using Voxta.Core;
using Voxta.Data.LiteDB;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Voxta.Host.AspNetCore.WebSockets.Utils;

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

        AddAllServices(services);
    }

    private static void AddAllServices(IServiceCollection services)
    {
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

        services.AddAzureSpeechService();
        textToSpeechRegistry.RegisterAzureSpeechService();
        speechToTextRegistry.RegisterAzureSpeechService();
    }
}