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
        services.AddScoped<ChatServicesFactories>();
        services.AddSingleton<ChatRepositories>();
        
        services.AddSingleton<PendingSpeechManager>();
        services.AddSingleton<Sanitizer>();
        services.AddSingleton<ExclusiveLocalInputManager>();
        
        return services;
    }

    public static SelectorRegistry<ITextGenService> AddTextGenRegistry(this IServiceCollection services)
    {
        var registry = new SelectorRegistry<ITextGenService>();
        services.AddSingleton<ISelectorFactory<ITextGenService>>(sp => new SelectorFactory<ITextGenService>(registry, sp));
        return registry;
    }
    
    public static SelectorRegistry<ITextToSpeechService> AddTextToSpeechRegistry(this IServiceCollection services)
    {
        var registry = new SelectorRegistry<ITextToSpeechService>();
        services.AddSingleton<ISelectorFactory<ITextToSpeechService>>(sp => new SelectorFactory<ITextToSpeechService>(registry, sp));
        return registry;
    }
    
    public static SelectorRegistry<IAnimationSelectionService> AddAnimationSelectorRegistry(this IServiceCollection services)
    {
        var registry = new SelectorRegistry<IAnimationSelectionService>();
        services.AddSingleton<ISelectorFactory<IAnimationSelectionService>>(sp => new SelectorFactory<IAnimationSelectionService>(registry, sp));
        return registry;
    }
}