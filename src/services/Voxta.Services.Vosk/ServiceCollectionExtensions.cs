using Voxta.Abstractions.Services;
using Voxta.Services.Vosk;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddVosk(this IServiceCollection services)
    {
        services.AddSingleton<IVoskModelDownloader, VoskModelDownloader>();
        services.AddTransient<VoskSpeechToText>();
    }
    
    public static void RegisterVosk(this IServiceRegistry<ISpeechToTextService> registry)
    {
        registry.Add<VoskSpeechToText>(VoskConstants.ServiceName);
    }
}
