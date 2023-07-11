using ChatMate.Abstractions.Repositories;
using ChatMate.Server.Samples;

namespace ChatMate.Server.Filters;

public class AutoRequestServicesStartupFilter : IStartupFilter
{
    private readonly IBotRepository _botsRepository;

    public AutoRequestServicesStartupFilter(IBotRepository botsRepository)
    {
        _botsRepository = botsRepository;
    }
    
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        Task.Run(async () =>
        {
            await _botsRepository.SaveBotAsync(Kally.Create());
            await _botsRepository.SaveBotAsync(Melly.Create());
            await _botsRepository.SaveBotAsync(Kate.Create());
        }).Wait();
        
        return next;
    }
}
