namespace ChatMate.Services.NovelAI;

[Serializable]
public class NovelAISettings
{
    public required string Token { get; set; }
    public string Model { get; set; } = "clio-v1";
}