using ChatMate.Abstractions.Model;

namespace ChatMate.Abstractions.Services;

public interface ITextGenService : IService
{
    ValueTask<TextData> GenerateReplyAsync(IReadOnlyChatSessionData chatSessionData, CancellationToken cancellationToken);
    int GetTokenCount(string message);
}