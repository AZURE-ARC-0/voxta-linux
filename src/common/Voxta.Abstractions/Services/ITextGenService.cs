using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Services;

public interface ITextGenService : IService
{
    ValueTask<string> GenerateReplyAsync(IReadOnlyChatSessionData chatSessionData, CancellationToken cancellationToken);
    int GetTokenCount(string message);
}