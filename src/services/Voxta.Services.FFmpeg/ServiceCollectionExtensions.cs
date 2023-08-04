using Voxta.Abstractions.Services;
using Voxta.Services.FFmpeg;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddFFmpeg(this IServiceCollection services)
    {
        services.AddScoped<FFmpegAudioConverter>();
        
        services.AddSingleton<FFmpegRecordingService>();
        services.AddTransient<FFmpegSpeechToText>();
    }
    
    public static void RegisterFFmpeg(this IServiceRegistry<ISpeechToTextService> registry)
    {
        registry.Add<FFmpegSpeechToText>(FFmpegConstants.ServiceName);
    }
}
