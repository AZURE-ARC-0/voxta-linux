using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Services;
using ChatMate.Services.NovelAI;
using Microsoft.Extensions.DependencyInjection;

namespace ChatMate.Data.Yaml;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNovelAI(this IServiceCollection services)
    {
        services.AddSingleton<NovelAITextGenClient>();
        services.AddSingleton<NovelAITextToSpeechClient>();
        return services;
    }
    
    public static void RegisterNovelAI(this ISelectorRegistry<ITextGenService> registry)
    {
        registry.Add<NovelAITextGenClient>("NovelAI");
    }
    
    public static void RegisterNovelAI(this ISelectorRegistry<ITextToSpeechService> registry)
    {
        registry.Add<NovelAITextToSpeechClient>("NovelAI");
    }
}