using ChatMate.Abstractions.Repositories;

namespace ChatMate.Services.KoboldAI;

[Serializable]
public class KoboldAISettings : ISettings
{
    public string Uri { get; set; } = "http://localhost:5001";
}