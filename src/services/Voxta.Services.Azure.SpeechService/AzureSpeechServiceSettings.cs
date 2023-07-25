using Voxta.Abstractions.Repositories;

namespace Voxta.Services.AzureSpeechService;

[Serializable]
public class AzureSpeechServiceSettings : SettingsBase
{
    public required string SubscriptionKey { get; set; }
    public required string Region { get; set; }
    public string? LogFilename { get; set; }
    public bool FilterProfanity { get; set; }
}