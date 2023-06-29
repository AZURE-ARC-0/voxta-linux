namespace ChatMate.Services.NovelAI;

[Serializable]
public class NovelAISettings
{
    public const string DefaultModel = "clio-v1";
    
    public required string Token { get; set; }
    public string Model { get; set; } = DefaultModel;
}