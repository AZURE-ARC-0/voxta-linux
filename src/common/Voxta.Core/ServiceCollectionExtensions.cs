using Voxta.Abstractions.Services;
using Voxta.Abstractions.System;
using Voxta.Core;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddVoxta(this IServiceCollection services)
    {
        services.AddScoped<UserConnectionFactory>();
        services.AddSingleton<IUserConnectionManager, UserConnectionManager>();
        services.AddScoped<ChatSessionFactory>();
        services.AddScoped<SpeechGeneratorFactory>();
        services.AddSingleton<PendingSpeechManager>();
        services.AddSingleton<Sanitizer>();
        services.AddSingleton<ITimeProvider, TimeProvider>();
    }
    
    public static IServiceDefinitionsRegistry AddServiceDefinitionsRegistry(this IServiceCollection services)
    {
        var registry = new ServiceDefinitionsRegistry();
        services.AddSingleton<IServiceDefinitionsRegistry>(registry);
        return registry;
    }
    
    public static IServiceRegistry<ISpeechToTextService> AddSpeechToTextRegistry(this IServiceCollection services)
    {
        var registry = new ServiceRegistry<ISpeechToTextService>();
        services.AddTransient<IServiceFactory<ISpeechToTextService>>(sp => new ServiceFactory<ISpeechToTextService>(registry, sp));
        return registry;
    }

    public static IServiceRegistry<ITextGenService> AddTextGenRegistry(this IServiceCollection services)
    {
        var registry = new ServiceRegistry<ITextGenService>();
        services.AddTransient<IServiceFactory<ITextGenService>>(sp => new ServiceFactory<ITextGenService>(registry, sp));
        return registry;
    }
    
    public static IServiceRegistry<ITextToSpeechService> AddTextToSpeechRegistry(this IServiceCollection services)
    {
        var registry = new ServiceRegistry<ITextToSpeechService>();
        services.AddTransient<IServiceFactory<ITextToSpeechService>>(sp => new ServiceFactory<ITextToSpeechService>(registry, sp));
        return registry;
    }
    
    public static IServiceRegistry<IActionInferenceService> AddActionInferenceRegistry(this IServiceCollection services)
    {
        var registry = new ServiceRegistry<IActionInferenceService>();
        services.AddTransient<IServiceFactory<IActionInferenceService>>(sp => new ServiceFactory<IActionInferenceService>(registry, sp));
        return registry;
    }
    
    public static IServiceRegistry<ISummarizationService> AddSummarizationRegistry(this IServiceCollection services)
    {
        var registry = new ServiceRegistry<ISummarizationService>();
        services.AddTransient<IServiceFactory<ISummarizationService>>(sp => new ServiceFactory<ISummarizationService>(registry, sp));
        return registry;
    }
}