using ChatMate.Abstractions.Model;

namespace ChatMate.Abstractions.Services;

public interface ITextGenService
{
    ValueTask<TextData> GenerateReplyAsync(IReadOnlyChatSessionData chatSessionData, CancellationToken cancellationToken);
    int GetTokenCount(string message);
}