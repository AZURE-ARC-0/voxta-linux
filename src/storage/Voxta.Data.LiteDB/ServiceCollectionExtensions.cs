
using Voxta.Abstractions.Repositories;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace Voxta.Data.LiteDB;

public static class ServiceCollectionExtensions
{
    public static void AddLiteDBRepositories(this IServiceCollection services)
    {
        services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase("Data/Voxta.db"));
        services.AddSingleton<LiteDBMigrations>();
        services.AddSingleton<IProfileRepository, ProfileLiteDBRepository>();
        services.AddSingleton<ISettingsRepository, SettingsLiteDBRepository>();
        services.AddSingleton<ICharacterRepository, CharacterLiteDBRepository>();
        services.AddSingleton<IChatRepository, ChatLiteDBRepository>();
        services.AddSingleton<IChatMessageRepository, ChatMessageLiteDBRepository>();
    }
}