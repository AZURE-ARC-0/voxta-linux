using ChatMate.Abstractions.Model;

namespace ChatMate.Abstractions.Services;

public interface IAnimationSelectionService
{
    ValueTask<string> SelectAnimationAsync(ChatSessionData chatSessionData, CancellationToken cancellationToken);
}