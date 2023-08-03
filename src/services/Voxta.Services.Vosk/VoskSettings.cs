using Voxta.Abstractions.Repositories;

namespace Voxta.Services.Vosk;

[Serializable]
public class VoskSettings : SettingsBase
{
    public string Model { get; set; } = "vosk-model-small-en-us-0.15";
    public string? ModelHash { get; set; } = "30f26242c4eb449f948e42cb302dd7a686cb29a3423a8367f99ff41780942498";
    public string[] IgnoredWords { get; set; } = { "huh", "the" };
}