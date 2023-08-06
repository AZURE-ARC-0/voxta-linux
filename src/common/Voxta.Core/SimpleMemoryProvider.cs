using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;

namespace Voxta.Core;

public class SimpleMemoryProvider : IMemoryProvider
{
    private readonly IMemoryRepository _memoryRepository;
    private MemoryBook[] _memories = Array.Empty<MemoryBook>();

    public SimpleMemoryProvider(IMemoryRepository memoryRepository)
    {
        _memoryRepository = memoryRepository;
    }

    public async Task Initialize(Guid characterId, ChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        _memories = await _memoryRepository.GetScopeMemoryBooksAsync(characterId, cancellationToken);
    }
    
    public void QueryMemoryFast(IChatInferenceData chat, List<MemoryItem> items)
    {
        throw new NotImplementedException();
    }
}