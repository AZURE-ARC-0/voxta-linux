namespace ChatMate.Server;

public interface ITextGenService
{
    ValueTask<string> GenerateTextAsync(ChatData chatData, string text);
}