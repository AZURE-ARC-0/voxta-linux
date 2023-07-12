using ChatMate.Abstractions.Repositories;

namespace ChatMate.Services.ElevenLabs;

[Serializable]
public class ElevenLabsSettings : SettingsBase
{
    public required string ApiKey { get; set; }
}