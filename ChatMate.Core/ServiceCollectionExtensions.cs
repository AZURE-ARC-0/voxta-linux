using ChatMate.Core;
using Microsoft.Extensions.DependencyInjection;

namespace ChatMate.Data.Yaml;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChatMate(this IServiceCollection services)
    {
        services.AddSingleton<UserConnectionFactory>();
        return services;
    }
}