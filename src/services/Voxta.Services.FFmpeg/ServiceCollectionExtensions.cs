using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;
using Voxta.Services.FFmpeg;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddFFmpeg(this IServiceCollection services)
    {
        services.AddScoped<IAudioConverter, FFmpegAudioConverter>();
        services.AddSingleton<IRecordingService, FFmpegRecordingService>();
        
        services.AddTransient<FFmpegSpeechToText>();
    }
    
    public static void RegisterFFmpeg(this IServiceDefinitionsRegistry registry)
    {
        registry.Add(new ServiceDefinition
        {
            ServiceName = FFmpegConstants.ServiceName,
            Label = "FFmpeg",
            TextGen = ServiceDefinitionCategoryScore.NotSupported,
            STT = ServiceDefinitionCategoryScore.Medium,
            TTS = ServiceDefinitionCategoryScore.NotSupported,
            Summarization = ServiceDefinitionCategoryScore.NotSupported,
            ActionInference = ServiceDefinitionCategoryScore.NotSupported,
            Features = new[] { ServiceFeatures.NSFW },
            Recommended = false,
            Notes = "Linux support.",
            SettingsType = typeof(FFmpegSettings),
        });
    }
    
    public static void RegisterFFmpeg(this IServiceRegistry<ISpeechToTextService> registry)
    {
        registry.Add<FFmpegSpeechToText>(FFmpegConstants.ServiceName);
    }
}
