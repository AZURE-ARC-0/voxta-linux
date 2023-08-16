using Voxta.Abstractions.Repositories;
using Voxta.Characters.Samples;
using Voxta.Data.LiteDB;

namespace Voxta.Server.Filters;

public class AutoRequestServicesStartupFilter : IStartupFilter
{
    private readonly LiteDBMigrations _migrations;
    private readonly ICharacterRepository _charactersRepository;
    private readonly IMemoryRepository _memoryRepository;

    public AutoRequestServicesStartupFilter(LiteDBMigrations migrations, ICharacterRepository charactersRepository, IMemoryRepository memoryRepository)
    {
        _migrations = migrations;
        _charactersRepository = charactersRepository;
        _memoryRepository = memoryRepository;
    }
    
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        Task.Run(async () =>
        {
            // Migrations
            await _migrations.MigrateAsync();
            
            // Default characters
            await _charactersRepository.SaveCharacterAsync(CatherineCharacter.Create());
            await _memoryRepository.SaveBookAsync(CatherineCharacter.CreateBook());
            
            await _charactersRepository.SaveCharacterAsync(GeorgeCharacter.Create());
            await _memoryRepository.SaveBookAsync(GeorgeCharacter.CreateBook());
        }).Wait();
        
        return next;
    }
}
