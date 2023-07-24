using Voxta.Abstractions.Repositories;

namespace Voxta.Services.Vosk;

[Serializable]
public class VoskSettings : SettingsBase
{
    public required string Model { get; set; }
    public string? ModelHash { get; set; }
}