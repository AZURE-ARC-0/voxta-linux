using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Services;
using ChatMate.Services.Vosk;

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
