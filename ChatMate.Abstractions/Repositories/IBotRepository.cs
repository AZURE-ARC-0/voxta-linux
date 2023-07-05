using ChatMate.Abstractions.Model;

namespace ChatMate.Abstractions.Repositories;

public interface IBotRepository
{
    Task<ServerWelcomeMessage.BotTemplate[]> GetBotsListAsync(CancellationToken cancellationToken);
    Task<BotDefinition?> GetBotAsync(string id, CancellationToken cancellationToken);
}