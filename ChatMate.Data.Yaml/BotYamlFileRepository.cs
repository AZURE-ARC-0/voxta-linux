using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;

namespace ChatMate.Data.Yaml;

public class BotYamlFileRepository : YamlFileRepositoryBase, IBotRepository
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private List<BotDefinition>? _bots;
    
    public async Task<ServerWelcomeMessage.BotTemplate[]> GetBotsListAsync(CancellationToken cancellationToken)
    {
        var bots = await LoadBotsCheckAsync(cancellationToken);
        return bots.Select(b => new ServerWelcomeMessage.BotTemplate
        {
            Id = b.Id,
            Name = b.Name,
            Description = b.Description,
        }).ToArray();
    }
    
    public async Task<BotDefinition?> GetBotAsync(string id, CancellationToken cancellationToken)
    {
        var bots = await LoadBotsCheckAsync(cancellationToken);
        if(!Guid.TryParse(id, out var guid)) return null;
        return bots.FirstOrDefault(b => b.Id == guid);
    }

    private async ValueTask<List<BotDefinition>> LoadBotsCheckAsync(CancellationToken cancellationToken)
    {
        var bots = _bots;
        if (bots != null) return bots;
        
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            bots = _bots;
            if (bots != null) return bots;
            
            bots = await LoadBotsAsync();
            _bots = bots;
            return bots;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static async ValueTask<List<BotDefinition>> LoadBotsAsync()
    {
        var bots = new List<BotDefinition>();
        if (!Directory.Exists("Data/Bots")) return bots;
        foreach(var file in Directory.EnumerateFiles("Data/Bots", "*.yaml"))
        {
            var bot = await DeserializeFileAsync<BotDefinition>(file);
            if (bot == null) continue;
            bots.Add(bot);
        }
        return bots;
    }
}