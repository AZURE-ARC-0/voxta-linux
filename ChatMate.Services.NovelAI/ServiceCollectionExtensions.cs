using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Services;
using ChatMate.Services.ElevenLabs;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNovelAI(this IServiceCollection services)
    {
        services.AddScoped<NovelAITextGenClient>();
        services.AddScoped<NovelAITextToSpeechClient>();
        return services;
    }
    
    public static void RegisterNovelAI(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<NovelAITextGenClient>(NovelAIConstants.ServiceName);
    }
    
    public static void RegisterNovelAI(this IServiceRegistry<ITextToSpeechService> registry)
    {
        registry.Add<NovelAITextToSpeechClient>(NovelAIConstants.ServiceName);
    }
}