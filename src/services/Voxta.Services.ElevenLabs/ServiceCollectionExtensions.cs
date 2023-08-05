using Voxta.Abstractions.Services;
using Voxta.Services.ElevenLabs;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddElevenLabs(this IServiceCollection services)
    {
        services.AddTransient<ElevenLabsTextToSpeechService>();
    }
    
    public static void RegisterElevenLabs(this IServiceRegistry<ITextToSpeechService> registry)
    {
        registry.Add<ElevenLabsTextToSpeechService>(ElevenLabsConstants.ServiceName);
    }
}