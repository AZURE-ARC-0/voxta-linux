using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Services;

public interface IMemoryProvider
{
    Task Initialize(Guid characterId, IChatTextProcessor textProcessor, CancellationToken cancellationToken);
    void QueryMemoryFast(ChatSessionData chat);
}