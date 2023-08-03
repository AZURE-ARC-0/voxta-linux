using Voxta.Abstractions.Repositories;

namespace Voxta.Services.KoboldAI;

[Serializable]
public class KoboldAISettings : SettingsBase
{
    public required string Uri { get; set; }
    public int MaxContextTokens { get; set; } = 1600;
    public KoboldAIParameters? Parameters { get; set; }
}