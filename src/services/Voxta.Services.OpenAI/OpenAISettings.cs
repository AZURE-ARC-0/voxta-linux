using Voxta.Abstractions.Repositories;

namespace Voxta.Services.OpenAI;

[Serializable]
public class OpenAISettings : SettingsBase
{
    public const string DefaultModel = "gpt-3.5-turbo";

    public required string ApiKey { get; set; }
    public string Model { get; set; } = DefaultModel;
}