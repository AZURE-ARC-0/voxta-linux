using Voxta.Abstractions.DependencyInjection;
using Voxta.Abstractions.Services;
using Voxta.Services.AzureSpeechService;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddAzureSpeechService(this IServiceCollection services)
    {
        services.AddTransient<AzureSpeechServiceSpeechToText>();
        services.AddTransient<AzureSpeechServiceTextToSpeech>();
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