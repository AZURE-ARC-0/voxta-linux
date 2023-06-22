namespace ChatMate.Server;

public interface ITextGenService
{
    ValueTask<TextData> GenerateReplyAsync(IReadOnlyChatData chatData);
    int GetTokenCount(string message);
}