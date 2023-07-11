using ChatMate.Abstractions.Repositories;

namespace ChatMate.Services.ElevenLabs;

[Serializable]
public class ElevenLabsSettings : ISettings
{
    public required string ApiKey { get; set; }
}