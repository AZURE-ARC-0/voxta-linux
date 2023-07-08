using ChatMate.Abstractions.Model;

namespace ChatMate.Abstractions.Services;

public interface IAnimationSelectionService : IService
{
    ValueTask<string> SelectAnimationAsync(ChatSessionData chatSessionData, CancellationToken cancellationToken);
}