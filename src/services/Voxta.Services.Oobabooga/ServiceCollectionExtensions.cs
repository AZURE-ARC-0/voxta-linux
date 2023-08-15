using Voxta.Abstractions.Services;
using Voxta.Services.Oobabooga;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddOobabooga(this IServiceCollection services)
    {
        services.AddTransient<OobaboogaTextGenService>();
        services.AddTransient<OobaboogaActionInferenceService>();
        services.AddTransient<OobaboogaSummarizationService>();
    }
    
    public static void RegisterOobabooga(this IServiceHelpRegistry registry)
    {
        registry.Add(new ServiceHelp
        {
            ServiceName = OobaboogaConstants.ServiceName,
            Label = "Oobabooga Text Generation Web UI",
            TextGen = true,
            STT = false,
            TTS = false,
            Summarization = true,
            ActionInference = true,
        });
    }
    
    public static void RegisterOobabooga(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<OobaboogaTextGenService>(OobaboogaConstants.ServiceName);
    }
    
    public static void RegisterOobabooga(this IServiceRegistry<IActionInferenceService> registry)
    {
        registry.Add<OobaboogaActionInferenceService>(OobaboogaConstants.ServiceName);
    }
    
    public static void RegisterOobabooga(this IServiceRegistry<ISummarizationService> registry)
    {
        registry.Add<OobaboogaSummarizationService>(OobaboogaConstants.ServiceName);
    }
}
