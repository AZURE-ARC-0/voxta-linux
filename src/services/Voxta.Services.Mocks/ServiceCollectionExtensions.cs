using Voxta.Abstractions.DependencyInjection;
using Voxta.Abstractions.Services;
using Voxta.Services.Mocks;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddMocks(this IServiceCollection services)
    {
        services.AddTransient<MockTextGenService>();
        services.AddTransient<MockTextToSpeechService>();
        services.AddTransient<MockActionInferenceService>();
    }
    
    public static void RegisterMocks(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<MockTextGenService>(MockConstants.ServiceName);
    }
    
    public static void RegisterMocks(this IServiceRegistry<ITextToSpeechService> registry)
    {
        registry.Add<MockTextToSpeechService>(MockConstants.ServiceName);
    }
    
    public static void RegisterMocks(this IServiceRegistry<IActionInferenceService> registry)
    {
        registry.Add<MockActionInferenceService>(MockConstants.ServiceName);
    }
}