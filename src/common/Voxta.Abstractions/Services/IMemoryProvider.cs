using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Services;

public interface IMemoryProvider
{
    Task Initialize(Guid characterId, ChatSessionData chatSessionData, CancellationToken cancellationToken);
    void QueryMemoryFast(ChatSessionData chat, List<MemoryItem> items);
}