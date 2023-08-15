#if(WINDOWS)
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
    
    public static void RegisterWindowsSpeech(this IServiceHelpRegistry registry)
    {
        registry.Add(new ServiceHelp
        {
            ServiceName = WindowsSpeechConstants.ServiceName,
            Label = "Windows Speech",
            TextGen = false,
            STT = true,
            TTS = true,
            Summarization = false,
            ActionInference = false,
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