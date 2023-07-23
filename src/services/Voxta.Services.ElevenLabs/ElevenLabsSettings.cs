using Voxta.Abstractions.Repositories;

namespace Voxta.Services.ElevenLabs;

[Serializable]
public class ElevenLabsSettings : SettingsBase
{
    public string? ApiKey { get; set; }
}