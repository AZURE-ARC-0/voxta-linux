using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Services;

public interface ITextGenService : IService
{
    ValueTask<string> GenerateReplyAsync(IChatInferenceData chat, CancellationToken cancellationToken);
    int GetTokenCount(string message);
}