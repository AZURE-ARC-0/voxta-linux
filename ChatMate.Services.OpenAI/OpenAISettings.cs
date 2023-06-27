namespace ChatMate.Services.OpenAI;

[Serializable]
public class OpenAISettings
{
    public required string ApiKey { get; set; }
    public string Model { get; set; } = "gpt-3.5-turbo";
}