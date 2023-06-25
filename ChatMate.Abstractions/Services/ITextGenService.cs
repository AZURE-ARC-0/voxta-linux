using ChatMate.Abstractions.Model;

namespace ChatMate.Abstractions.Services;

public interface ITextGenService
{
    ValueTask<TextData> GenerateReplyAsync(IReadOnlyChatData chatData);
    int GetTokenCount(string message);
}