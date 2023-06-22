using YamlDotNet.Serialization;

namespace ChatMate.Server;

public interface IBotRepository
{
    Task<ServerBotsListMessage.Bot[]> GetBotsListAsync(CancellationToken cancellationToken);
    Task<BotDefinition?> GetBotAsync(string id, CancellationToken cancellationToken);
}

public class BotYamlFileRepository : IBotRepository
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .Build();
    
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private List<BotDefinition>? _bots;
    
    public async Task<ServerBotsListMessage.Bot[]> GetBotsListAsync(CancellationToken cancellationToken)
    {
        var bots = await LoadBotsCheckAsync(cancellationToken);
        return bots.Select(b => new ServerBotsListMessage.Bot
        {
            Id = b.Name,
            Name = b.Name,
            Description = b.Description,
        }).ToArray();
    }
    
    public async Task<BotDefinition?> GetBotAsync(string id, CancellationToken cancellationToken)
    {
        var bots = await LoadBotsCheckAsync(cancellationToken);
        return bots.FirstOrDefault(b => b.Name == id);
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
        foreach(var file in Directory.EnumerateFiles("Bots", "*.yaml"))
        {
            await using var stream = File.OpenRead(file);
            using var reader = new StreamReader(stream);
            var bot = YamlDeserializer.Deserialize<BotDefinition>(reader);
            bots.Add(bot);
        }
        return bots;
    }
}