using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Services;
using ChatMate.Services.ElevenLabs;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddElevenLabs(this IServiceCollection services)
    {
        services.AddScoped<ElevenLabsTextToSpeechClient>();
        return services;
    }
    
    public static void RegisterElevenLabs(this IServiceRegistry<ITextToSpeechService> registry)
    {
        registry.Add<ElevenLabsTextToSpeechClient>(ElevenLabsConstants.ServiceName);
    }
}