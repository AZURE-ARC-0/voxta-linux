using ChatMate.Abstractions.Model;

namespace ChatMate.Abstractions.Repositories;

public interface IBotRepository
{
    Task<ServerBotsListMessage.Bot[]> GetBotsListAsync(CancellationToken cancellationToken);
    Task<BotDefinition?> GetBotAsync(string id, CancellationToken cancellationToken);
}