using Voxta.Abstractions.DependencyInjection;
using Voxta.Abstractions.Services;
using Voxta.Services.Mocks;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddMocks(this IServiceCollection services)
    {
        services.AddTransient<MockTextGenClient>();
        services.AddTransient<MockTextToSpeechClient>();
        services.AddTransient<MockActionInferenceClient>();
    }
    
    public static void RegisterMocks(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<MockTextGenClient>(MockConstants.ServiceName);
    }
    
    public static void RegisterMocks(this IServiceRegistry<ITextToSpeechService> registry)
    {
        registry.Add<MockTextToSpeechClient>(MockConstants.ServiceName);
    }
    
    public static void RegisterMocks(this IServiceRegistry<IActionInferenceService> registry)
    {
        registry.Add<MockActionInferenceClient>(MockConstants.ServiceName);
    }
}