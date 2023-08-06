using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Repositories;

public interface IMemoryRepository
{
    Task<MemoryBook[]> GetScopeMemoryBooksAsync(Guid charId, CancellationToken cancellationToken);   
}