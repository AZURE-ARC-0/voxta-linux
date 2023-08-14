using Voxta.Shared.LLMUtils;

namespace Voxta.Services.NovelAI;

[Serializable]
public class NovelAISettings : LLMSettingsBase<NovelAIParameters>
{
    public const string ClioV1 = "clio-v1";
    public const string KayraV1 = "kayra-v1";
    
    public const string DefaultModel = KayraV1;

    public required string Token { get; set; }
    public string Model { get; set; } = DefaultModel;
    
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
}