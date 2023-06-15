namespace ChatMate.Server;

public interface ITextGenService
{
    ValueTask<ChatMessageData> GenerateReplyAsync(IReadOnlyChatData chatData);
    int GetTokenCount(TextData message);
}