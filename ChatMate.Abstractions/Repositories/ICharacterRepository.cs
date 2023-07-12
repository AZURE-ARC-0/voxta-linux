using ChatMate.Abstractions.Model;

namespace ChatMate.Abstractions.Repositories;

public interface ICharacterRepository
{
    Task<ServerWelcomeMessage.CharactersListItem[]> GetCharactersListAsync(CancellationToken cancellationToken);
    Task<Character?> GetCharacterAsync(string id, CancellationToken cancellationToken);
    Task SaveCharacterAsync(Character card);
}