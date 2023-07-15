using ChatMate.Abstractions.Repositories;

namespace ChatMate.Services.ElevenLabs;

[Serializable]
public class ElevenLabsSettings : SettingsBase
{
    public string? ApiKey { get; set; }
}