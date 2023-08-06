using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Services;

public interface IMemoryProvider
{
    Task Initialize(Guid characterId, ChatSessionData chatSessionData, CancellationToken cancellationToken);
    void QueryMemoryFast(IChatInferenceData chat, List<MemoryItem> items);
}