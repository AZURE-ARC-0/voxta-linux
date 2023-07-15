using ChatMate.Abstractions.Model;

namespace ChatMate.Abstractions.Services;

public interface IActionInferenceService : IService
{
    ValueTask<string> SelectActionAsync(ChatSessionData chatSessionData, CancellationToken cancellationToken);
}