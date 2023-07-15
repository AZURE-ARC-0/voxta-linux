using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Services;
using ChatMate.Services.Fakes;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddFakes(this IServiceCollection services)
    {
        services.AddTransient<FakesTextGenClient>();
        services.AddTransient<FakesTextToSpeechClient>();
    }
    
    public static void RegisterFakes(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<FakesTextGenClient>(FakesConstants.ServiceName);
    }
    
    public static void RegisterFakes(this IServiceRegistry<ITextToSpeechService> registry)
    {
        registry.Add<FakesTextToSpeechClient>(FakesConstants.ServiceName);
    }
    
    public static void RegisterFakes(this IServiceRegistry<IActionInferenceService> registry)
    {
        registry.Add<FakesActionInferenceClient>(FakesConstants.ServiceName);
    }
}