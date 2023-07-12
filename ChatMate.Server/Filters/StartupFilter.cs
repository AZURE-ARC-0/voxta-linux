using ChatMate.Abstractions.Repositories;
using ChatMate.Server.Samples;

namespace ChatMate.Server.Filters;

public class AutoRequestServicesStartupFilter : IStartupFilter
{
    private readonly ICharacterRepository _charactersRepository;

    public AutoRequestServicesStartupFilter(ICharacterRepository charactersRepository)
    {
        _charactersRepository = charactersRepository;
    }
    
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        Task.Run(async () =>
        {
            await _charactersRepository.SaveCharacterAsync(Kally.Create());
            await _charactersRepository.SaveCharacterAsync(Melly.Create());
            await _charactersRepository.SaveCharacterAsync(Kate.Create());
        }).Wait();
        
        return next;
    }
}
