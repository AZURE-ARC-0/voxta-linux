using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Repositories;

public interface ICharacterRepository
{
    Task<ServerCharactersListLoadedMessage.CharactersListItem[]> GetCharactersListAsync(CancellationToken cancellationToken);
    Task<Character?> GetCharacterAsync(Guid id, CancellationToken cancellationToken);
    Task SaveCharacterAsync(Character character);
    Task DeleteAsync(Guid charId);
}