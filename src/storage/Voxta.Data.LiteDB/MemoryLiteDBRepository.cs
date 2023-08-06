
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using LiteDB;

namespace Voxta.Data.LiteDB;

public class MemoryLiteDBRepository : IMemoryRepository
{
    private readonly ILiteCollection<MemoryBook> _memoryBooksRepository;

    public MemoryLiteDBRepository(ILiteDatabase db)
    {
        _memoryBooksRepository = db.GetCollection<MemoryBook>();
    }

    public Task<MemoryBook[]> GetScopeMemoryBooksAsync(Guid charId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_memoryBooksRepository
            .Query()
            .Where(x => x.CharacterId == charId)
            .ToArray()
        );
    }
}