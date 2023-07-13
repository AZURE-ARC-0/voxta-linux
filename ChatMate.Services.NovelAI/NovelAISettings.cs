using ChatMate.Abstractions.Repositories;

namespace ChatMate.Services.NovelAI;

[Serializable]
public class NovelAISettings : SettingsBase
{
    public const string DefaultModel = "clio-v1";

    public string? Token { get; set; }
    public string Model { get; set; } = DefaultModel;
}