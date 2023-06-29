using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Services;
using ChatMate.Services.Vosk;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVosk(this IServiceCollection services, IConfigurationSection configuration)
    {
        services.Configure<VoskOptions>(configuration);
        services.AddSingleton<IVoskModelDownloader, VoskModelDownloader>();
        services.AddSingleton<ISpeechRecognitionService, VoskSpeechRecognition>();
        return services;
    }
}

[Serializable]
public class VoskOptions
{
    public int LogLevel { get; init; } = -1;
    public required string Model { get; init; } = "vosk-model-small-en-us-0.15";
    public string? ModelZipHash { get; set; }
}