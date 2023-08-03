using Voxta.Abstractions.Repositories;

namespace Voxta.Services.Oobabooga;

[Serializable]
public class OobaboogaSettings : SettingsBase
{
    public required string Uri { get; set; }
    public int MaxContextTokens { get; set; } = 1600;
    public OobaboogaParameters? Parameters { get; set; }
}