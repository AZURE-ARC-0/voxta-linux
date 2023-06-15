namespace ChatMate.Server;

public interface ITextToSpeechService
{
    Task GenerateSpeechAsync(SpeechRequest request, HttpResponse response, string extension);
}