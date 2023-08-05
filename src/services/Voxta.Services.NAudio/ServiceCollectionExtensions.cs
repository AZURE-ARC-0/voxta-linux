using Voxta.Abstractions.Services;
using Voxta.Services.NAudio;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddNAudio(this IServiceCollection services)
    {
        services.AddScoped<IAudioConverter, NAudioAudioConverter>();
        services.AddSingleton<IRecordingService, NAudioRecordingService>();
    }
}