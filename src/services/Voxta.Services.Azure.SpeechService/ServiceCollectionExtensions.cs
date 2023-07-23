using Voxta.Abstractions.DependencyInjection;
using Voxta.Abstractions.Services;
using Voxta.Services.OpenAI;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddAzureSpeechService(this IServiceCollection services)
    {
        services.AddTransient<AzureSpeechServiceSpeechToText>();
    }
    
    public static void RegisterAzureSpeechService(this IServiceRegistry<ISpeechToTextService> registry)
    {
        registry.Add<AzureSpeechServiceSpeechToText>(AzureSpeechServiceConstants.ServiceName);
    }
}