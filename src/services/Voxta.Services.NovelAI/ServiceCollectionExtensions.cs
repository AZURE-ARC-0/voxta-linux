using Voxta.Abstractions.DependencyInjection;
using Voxta.Abstractions.Services;
using Voxta.Services.NovelAI;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddNovelAI(this IServiceCollection services)
    {
        services.AddTextToSpeechUtils();
        services.AddTransient<NovelAITextGenClient>();
        services.AddTransient<NovelAITextToSpeechClient>();
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