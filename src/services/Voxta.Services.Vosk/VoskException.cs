namespace Voxta.Services.Vosk;

public class VoskException : Exception
{
    public VoskException(string message) : base(message)
    {
    }
}