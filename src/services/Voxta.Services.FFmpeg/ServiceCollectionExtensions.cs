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
    
    public static void RegisterFFmpeg(this IServiceRegistry<ISpeechToTextService> registry)
    {
        registry.Add<FFmpegSpeechToText>(FFmpegConstants.ServiceName);
    }
}
