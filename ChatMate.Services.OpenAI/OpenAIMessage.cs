namespace ChatMate.Services.OpenAI;

[Serializable]
public class OpenAIMessage
{
    public string role { get; set; }
    public string content { get; set; }
}