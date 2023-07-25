using Voxta.Abstractions.Repositories;

namespace Voxta.Services.NovelAI;

[Serializable]
public class NovelAISettings : SettingsBase
{
    public const string DefaultModel = "clio-v1";

    public required string Token { get; set; }
    public string Model { get; set; } = DefaultModel;
    public NovelAIParameters? Parameters { get; set; }
}