namespace ChatMate.Server;

public interface ITextGenService
{
    ValueTask<string> GenerateReplyAsync(ChatData chatData);
}