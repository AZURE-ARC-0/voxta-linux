
using ChatMate.Abstractions.Repositories;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace ChatMate.Data.LiteDB;

public static class ServiceCollectionExtensions
{
    public static void AddLiteDBRepositories(this IServiceCollection services)
    {
        services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase("Data/ChatMate.db"));
        services.AddSingleton<LiteDBMigrations>();
        services.AddSingleton<ICharacterRepository, CharacterLiteDBRepository>();
        services.AddSingleton<ISettingsRepository, SettingsLiteDBRepository>();
        services.AddSingleton<IProfileRepository, ProfileLiteDBRepository>();
    }
}