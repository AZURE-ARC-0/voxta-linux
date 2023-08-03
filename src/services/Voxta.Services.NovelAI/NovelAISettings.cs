using Voxta.Abstractions.Repositories;

namespace Voxta.Services.NovelAI;

[Serializable]
public class NovelAISettings : SettingsBase
{
    public const string DefaultModel = "clio-v1";

    public required string Token { get; set; }
    public string Model { get; set; } = DefaultModel;
    public int MaxContextTokens { get; set; } = 1600;
    
    public string[] ThinkingSpeech { get; set; } = {
        "m",
        "uh",
        "..",
        "...",
        "mmh",
        "hum",
        "huh",
        "!!",
        "??",
        "o",
    };
    
    public NovelAIParameters? Parameters { get; set; }
}