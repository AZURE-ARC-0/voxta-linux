
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using LiteDB;

namespace Voxta.Data.LiteDB;

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
            .Select(x => new { x.Id, x.Name, x.CreatorNotes, x.Services, x.ReadOnly })
            .ToList();
        
        var result = cards.Select(b => new ServerWelcomeMessage.CharactersListItem
        {
            Id = b.Id ?? throw new NullReferenceException("Character card ID was null"),
            Name = b.Name,
            Description = $"{b.CreatorNotes?[..Math.Min(50, b.CreatorNotes.Length)]} (Text: {b.Services.TextGen.Service}, TTS: {b.Services.SpeechGen.Service})",
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

    public Task DeleteAsync(string charId)
    {
        _charactersCollection.DeleteMany(x => x.Id == charId);
        return Task.CompletedTask;
    }
}