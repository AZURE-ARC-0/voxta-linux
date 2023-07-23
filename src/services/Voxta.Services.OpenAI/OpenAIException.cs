namespace ChatMate.Services.OpenAI;

public class OpenAIException : Exception
{
    public OpenAIException(string message) : base(message)
    {
    }
}