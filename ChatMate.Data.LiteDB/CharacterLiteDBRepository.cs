
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;
using LiteDB;

namespace ChatMate.Data.LiteDB;

public class CharacterLiteDBRepository : ICharacterRepository
{
    private readonly ILiteCollection<Character> _charactersCollection;

    public CharacterLiteDBRepository(ILiteDatabase db)
    {
        _charactersCollection = db.GetCollection<Character>();
    }
    
    public Task<ServerWelcomeMessage.CharactersListItem[]> GetCharactersListAsync(CancellationToken cancellationToken)
    {
        var cards = _charactersCollection.Query()
            .Select(x => new { x.Id, x.Name, x.Description, x.ReadOnly })
            .ToList();
        
        var result = cards.Select(b => new ServerWelcomeMessage.CharactersListItem
        {
            Id = b.Id ?? throw new NullReferenceException("Character card ID was null"),
            Name = b.Name,
            Description = b.Description,
            ReadOnly = b.ReadOnly,
        }).ToArray();

        return Task.FromResult(result);
    }
    
    public Task<Character?> GetCharacterAsync(string id, CancellationToken cancellationToken)
    {
        var character = _charactersCollection.FindOne(x => x.Id == id);
        return Task.FromResult<Character?>(character);
    }

    public Task SaveCharacterAsync(Character card)
    {
        _charactersCollection.Upsert(card);
        return Task.CompletedTask;
    }
}