using Voxta.Abstractions.Repositories;

namespace Voxta.Services.NovelAI;

[Serializable]
public class NovelAISettings : SettingsBase
{
    public const string ClioV1 = "clio-v1";
    public const string KayraV1 = "kayra-v1";
    
    public const string DefaultModel = KayraV1;

    public required string Token { get; set; }
    public string Model { get; set; } = DefaultModel;
    public int MaxMemoryTokens { get; set; } = 400;
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