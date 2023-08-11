
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

    public Task<MemoryBook[]> GetScopeMemoryBooksAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_memoryBooksRepository
            .Query()
            .Where(x => x.CharacterId == characterId)
            .ToArray()
        );
    }

    public Task<MemoryBook?> GetCharacterBookAsync(Guid characterId)
    {
        return Task.FromResult<MemoryBook?>(_memoryBooksRepository.FindOne(x => x.CharacterId == characterId));
    }

    public Task SaveBookAsync(MemoryBook book)
    {
        _memoryBooksRepository.Upsert(book);
        return Task.CompletedTask;
    }
}