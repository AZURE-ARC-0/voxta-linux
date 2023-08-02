#if(WINDOWS)
using Voxta.Abstractions.Services;
using Voxta.Services.WindowsSpeech;
using Voxta.Shared.TextToSpeechUtils;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddWindowsSpeech(this IServiceCollection services)
    {
        services.AddTextToSpeechUtils();
        services.AddTransient<WindowsSpeechTextToSpeechService>();
        services.AddTransient<WindowsSpeechSpeechToText>();
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