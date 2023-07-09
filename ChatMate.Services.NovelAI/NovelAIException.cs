namespace ChatMate.Services.ElevenLabs;

public class NovelAIException : Exception
{
    public NovelAIException(string message) : base(message)
    {
    }
}