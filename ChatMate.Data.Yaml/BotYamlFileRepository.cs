using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;

namespace ChatMate.Data.Yaml;

public class BotYamlFileRepository : YamlFileRepositoryBase, IBotRepository
{
    public async Task<ServerWelcomeMessage.BotTemplate[]> GetBotsListAsync(CancellationToken cancellationToken)
    {
        var bots = await LoadBotsCheckAsync(cancellationToken);
        return bots.Select(b => new ServerWelcomeMessage.BotTemplate
        {
            Id = b.Id ?? throw new NullReferenceException("Bot ID was null"),
            Name = b.Name,
            Description = b.Description,
        }).ToArray();
    }
    
    public async Task<BotDefinition?> GetBotAsync(string id, CancellationToken cancellationToken)
    {
        var bots = await LoadBotsCheckAsync(cancellationToken);
        return bots.FirstOrDefault(b => b.Id == id);
    }

    public async Task SaveBotAsync(BotDefinition bot)
    {
        IsValidFileName(bot.Id);
        Directory.CreateDirectory("Data/Bots");
        await SerializeFileAsync("Data/Bots/" + bot.Id, bot);
    }

    private static async ValueTask<List<BotDefinition>> LoadBotsCheckAsync(CancellationToken cancellationToken)
    {
        var bots = new List<BotDefinition>();
        if (!Directory.Exists("Data/Bots")) return bots;
        foreach(var file in Directory.EnumerateFiles("Data/Bots", "*.yaml"))
        {
            var bot = await DeserializeFileAsync<BotDefinition>(file, cancellationToken);
            if (bot == null) continue;
            bot.Id = Path.GetFileName(file);
            bots.Add(bot);
        }
        return bots;
    }
}