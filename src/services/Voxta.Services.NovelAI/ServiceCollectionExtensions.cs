using Voxta.Abstractions.DependencyInjection;
using Voxta.Abstractions.Services;
using Voxta.Services.NovelAI;
using Voxta.Shared.TextToSpeechUtils;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddNovelAI(this IServiceCollection services)
    {
        services.AddTextToSpeechUtils();
        services.AddTransient<NovelAITextGenService>();
        services.AddTransient<NovelAITextToSpeechService>();
        services.AddTransient<NovelAIActionInferenceService>();
    }
    
    public static void RegisterNovelAI(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<NovelAITextGenService>(NovelAIConstants.ServiceName);
    }
    
    public static void RegisterNovelAI(this IServiceRegistry<ITextToSpeechService> registry)
    {
        registry.Add<NovelAITextToSpeechService>(NovelAIConstants.ServiceName);
    }
    
    public static void RegisterNovelAI(this IServiceRegistry<IActionInferenceService> registry)
    {
        registry.Add<NovelAIActionInferenceService>(NovelAIConstants.ServiceName);
    }
}