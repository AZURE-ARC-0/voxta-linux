using ChatMate.Abstractions.Model;

namespace ChatMate.Abstractions.Services;

public interface ITextGenService
{
    ValueTask<TextData> GenerateReplyAsync(IReadOnlyChatSessionData chatSessionData);
    int GetTokenCount(string message);
}