namespace Voxta.Services.KoboldAI;

public class RemoteServiceException : Exception
{
    public RemoteServiceException(string message) : base(message)
    {
    }
}