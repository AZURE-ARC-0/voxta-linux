using ChatMate.Abstractions.Repositories;

namespace ChatMate.Services.OpenAI;

[Serializable]
public class OpenAISettings : ISettings
{
    public const string DefaultModel = "gpt-3.5-turbo";

    public required string ApiKey { get; set; }
    public string Model { get; set; } = DefaultModel;
}