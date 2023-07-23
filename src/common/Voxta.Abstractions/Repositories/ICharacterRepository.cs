using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Repositories;

public interface ICharacterRepository
{
    Task<ServerWelcomeMessage.CharactersListItem[]> GetCharactersListAsync(CancellationToken cancellationToken);
    Task<Character?> GetCharacterAsync(string id, CancellationToken cancellationToken);
    Task SaveCharacterAsync(Character card);
    Task DeleteAsync(string charId);
}