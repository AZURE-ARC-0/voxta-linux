using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Services;

public interface IActionInferenceService : IService
{
    ValueTask<string> SelectActionAsync(IChatInferenceData chat, CancellationToken cancellationToken);
}