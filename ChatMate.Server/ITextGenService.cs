namespace ChatMate.Server;

public interface ITextGenService
{
    ValueTask<string> GenerateTextAsync(string text);
}