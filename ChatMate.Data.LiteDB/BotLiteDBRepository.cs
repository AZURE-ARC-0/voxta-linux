
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;
using LiteDB;

namespace ChatMate.Data.LiteDB;

public class BotLiteDBRepository : IBotRepository
{
    private readonly ILiteCollection<BotDefinition> _botsCollection;

    public BotLiteDBRepository(ILiteDatabase db)
    {
        _botsCollection = db.GetCollection<BotDefinition>();
    }
    
    public Task<ServerWelcomeMessage.BotTemplate[]> GetBotsListAsync(CancellationToken cancellationToken)
    {
        var bots = _botsCollection.Query()
            .Select(x => new { x.Id, x.Name, x.Description, x.ReadOnly })
            .ToList();
        
        var result = bots.Select(b => new ServerWelcomeMessage.BotTemplate
        {
            Id = b.Id ?? throw new NullReferenceException("Bot ID was null"),
            Name = b.Name,
            Description = b.Description,
            ReadOnly = b.ReadOnly,
        }).ToArray();

        return Task.FromResult(result);
    }
    
    public Task<BotDefinition?> GetBotAsync(string id, CancellationToken cancellationToken)
    {
        var bot = _botsCollection.FindOne(x => x.Id == id);
        return Task.FromResult<BotDefinition?>(bot);
    }

    public Task SaveBotAsync(BotDefinition bot)
    {
        _botsCollection.Upsert(bot);
        return Task.CompletedTask;
    }
}