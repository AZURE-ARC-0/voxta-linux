namespace ChatMate.Services.ElevenLabs;

public class ElevenLabsException : Exception
{
    public ElevenLabsException(string message) : base(message)
    {
    }
}