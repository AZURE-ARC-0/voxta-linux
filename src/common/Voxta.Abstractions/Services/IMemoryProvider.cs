using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Services;

public interface IMemoryProvider
{
    Task Initialize(Guid characterId, ChatSessionData chat, CancellationToken cancellationToken);
    void QueryMemoryFast(ChatSessionData chat);
}