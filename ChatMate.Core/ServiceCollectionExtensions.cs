using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Services;
using ChatMate.Common;
using ChatMate.Core;
using Microsoft.Extensions.DependencyInjection;

namespace ChatMate.Data.Yaml;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChatMate(this IServiceCollection services)
    {
        services.AddScoped<UserConnectionFactory>();
        services.AddScoped<ChatSessionFactory>();
        services.AddScoped<SpeechGeneratorFactory>();
        services.AddSingleton<ChatRepositories>();
        
        services.AddSingleton<PendingSpeechManager>();
        services.AddSingleton<Sanitizer>();
        services.AddSingleton<ExclusiveLocalInputManager>();
        
        return services;
    }

    public static ServiceRegistry<ITextGenService> AddTextGenRegistry(this IServiceCollection services)
    {
        var registry = new ServiceRegistry<ITextGenService>();
        services.AddScoped<IServiceFactory<ITextGenService>>(sp => new ServiceFactory<ITextGenService>(registry, sp));
        return registry;
    }
    
    public static ServiceRegistry<ITextToSpeechService> AddTextToSpeechRegistry(this IServiceCollection services)
    {
        var registry = new ServiceRegistry<ITextToSpeechService>();
        services.AddScoped<IServiceFactory<ITextToSpeechService>>(sp => new ServiceFactory<ITextToSpeechService>(registry, sp));
        return registry;
    }
    
    public static ServiceRegistry<IAnimationSelectionService> AddAnimationServiceRegistry(this IServiceCollection services)
    {
        var registry = new ServiceRegistry<IAnimationSelectionService>();
        services.AddScoped<IServiceFactory<IAnimationSelectionService>>(sp => new ServiceFactory<IAnimationSelectionService>(registry, sp));
        return registry;
    }
}