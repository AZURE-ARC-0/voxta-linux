using Voxta.Services.TextToSpeechUtils;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddTextToSpeechUtils(this IServiceCollection services)
    {
        services.AddSingleton<ITextToSpeechPreprocessor, TextToSpeechPreprocessor>();
    }
}
