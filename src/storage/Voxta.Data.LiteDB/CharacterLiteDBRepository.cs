
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using LiteDB;

namespace Voxta.Data.LiteDB;

public class CharacterLiteDBRepository : ICharacterRepository
{
    private readonly IChatRepository _chatRepository;
    private readonly ILiteCollection<Character> _charactersCollection;

    public CharacterLiteDBRepository(ILiteDatabase db, IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
        _charactersCollection = db.GetCollection<Character>();
    }
    
    public Task<ServerCharactersListLoadedMessage.CharactersListItem[]> GetCharactersListAsync(CancellationToken cancellationToken)
    {
        var cards = _charactersCollection.Query()
            .Select(x => new { x.Id, x.Name, x.CreatorNotes, x.Services, x.ReadOnly, x.Culture, x.Prerequisites })
            .ToList();

        var result = cards
            .Select(b => new ServerCharactersListLoadedMessage.CharactersListItem
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.CreatorNotes?[..Math.Min(50, b.CreatorNotes.Length)],
                ReadOnly = b.ReadOnly,
                Culture = b.Culture,
                // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
                Prerequisites = b.Prerequisites ?? Array.Empty<string>(),
            })
            .OrderBy(x => x.Name)
            .ToArray();

        return Task.FromResult(result);
    }
    
    public Task<Character?> GetCharacterAsync(Guid id, CancellationToken cancellationToken)
    {
        var character = _charactersCollection.FindOne(x => x.Id == id);
        return Task.FromResult<Character?>(character);
    }

    public Task SaveCharacterAsync(Character character)
    {
        _charactersCollection.Upsert(character);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid charId)
    {
        foreach (var chat in await _chatRepository.GetChatsListAsync(charId, CancellationToken.None))
        {
            await _chatRepository.DeleteAsync(chat.Id);
        }
        _charactersCollection.DeleteMany(x => x.Id == charId);
    }
}