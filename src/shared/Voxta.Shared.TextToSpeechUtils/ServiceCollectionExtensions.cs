using Microsoft.Extensions.DependencyInjection;

namespace Voxta.Shared.TextToSpeechUtils;

public static class ServiceCollectionExtensions
{
    public static void AddTextToSpeechUtils(this IServiceCollection services)
    {
        services.AddSingleton<ITextToSpeechPreprocessor, TextToSpeechPreprocessor>();
    }
}
