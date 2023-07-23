using Voxta.Abstractions.Repositories;

namespace Voxta.Services.AzureSpeechService;

[Serializable]
public class AzureSpeechServiceSettings : SettingsBase
{
    public string? SubscriptionKey { get; set; }
    public string? Region { get; set; }
}