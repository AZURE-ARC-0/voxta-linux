using Voxta.Abstractions.Repositories;

namespace Voxta.Services.KoboldAI;

[Serializable]
public class KoboldAISettings : SettingsBase
{
    public required string Uri { get; set; }
}