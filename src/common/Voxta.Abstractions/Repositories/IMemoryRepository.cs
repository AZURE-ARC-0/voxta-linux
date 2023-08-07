using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Repositories;

public interface IMemoryRepository
{
    Task<MemoryBook[]> GetScopeMemoryBooksAsync(Guid characterId, CancellationToken cancellationToken);
    Task<MemoryBook> GetCharacterBookAsync(Guid characterId);
    Task SaveBookAsync(MemoryBook book);
}