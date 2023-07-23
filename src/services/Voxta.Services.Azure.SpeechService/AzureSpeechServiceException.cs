namespace Voxta.Services.AzureSpeechService;

public class AzureSpeechServiceException : Exception
{
    public AzureSpeechServiceException(string message) : base(message)
    {
    }
}