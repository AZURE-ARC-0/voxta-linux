namespace Voxta.Services.OpenAI;

public class AzureSpeechServiceException : Exception
{
    public AzureSpeechServiceException(string message) : base(message)
    {
    }
}