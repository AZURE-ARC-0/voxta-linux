namespace ChatMate.Server;

public interface ITextToSpeechService
{
    ValueTask<string> GenerateSpeechUrlAsync(string text);
    Task HandleSpeechProxyRequestAsync(HttpResponse response, Guid id, string extension);
}