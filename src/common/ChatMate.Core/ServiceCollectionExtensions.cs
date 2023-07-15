using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Services;
using ChatMate.Common;
using Microsoft.Extensions.DependencyInjection;

namespace ChatMate.Core;

public static class ServiceCollectionExtensions
{
    public static void AddChatMate(this IServiceCollection services)
    {
        services.AddScoped<UserConnectionFactory>();
        services.AddScoped<ChatSessionFactory>();
        services.AddScoped<SpeechGeneratorFactory>();
        
        services.AddSingleton<PendingSpeechManager>();
        services.AddSingleton<Sanitizer>();
    }
    
    public static ServiceRegistry<ISpeechToTextService> AddSpeechToTextRegistry(this IServiceCollection services)
    {
        var registry = new ServiceRegistry<ISpeechToTextService>();
        services.AddTransient<IServiceFactory<ISpeechToTextService>>(sp => new ServiceFactory<ISpeechToTextService>(registry, sp));
        return registry;
    }

    public static ServiceRegistry<ITextGenService> AddTextGenRegistry(this IServiceCollection services)
    {
        var registry = new ServiceRegistry<ITextGenService>();
        services.AddTransient<IServiceFactory<ITextGenService>>(sp => new ServiceFactory<ITextGenService>(registry, sp));
        return registry;
    }
    
    public static ServiceRegistry<ITextToSpeechService> AddTextToSpeechRegistry(this IServiceCollection services)
    {
        var registry = new ServiceRegistry<ITextToSpeechService>();
        services.AddTransient<IServiceFactory<ITextToSpeechService>>(sp => new ServiceFactory<ITextToSpeechService>(registry, sp));
        return registry;
    }
    
    public static ServiceRegistry<IActionInferenceService> AddActionInferenceRegistry(this IServiceCollection services)
    {
        var registry = new ServiceRegistry<IActionInferenceService>();
        services.AddTransient<IServiceFactory<IActionInferenceService>>(sp => new ServiceFactory<IActionInferenceService>(registry, sp));
        return registry;
    }
}