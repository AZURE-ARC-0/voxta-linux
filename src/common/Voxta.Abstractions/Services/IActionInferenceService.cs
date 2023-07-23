using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Services;

public interface IActionInferenceService : IService
{
    ValueTask<string> SelectActionAsync(ChatSessionData chatSessionData, CancellationToken cancellationToken);
}