using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.System;
using Voxta.Services.AzureSpeechService;

namespace Voxta.Server.ViewModels.ServiceSettings;

[Serializable]
public class AzureSpeechServiceSettingsViewModel : ServiceSettingsViewModel
{
    public required string SubscriptionKey { get; init; }
    public required string Region { get; init; }
    public string? LogFilename { get; init; }
    public bool FilterProfanity { get; init; }
    public bool Diarization { get; init; }

    public AzureSpeechServiceSettingsViewModel()
    {
    }

    [SetsRequiredMembers]
    public AzureSpeechServiceSettingsViewModel(ConfiguredService<AzureSpeechServiceSettings> source, ILocalEncryptionProvider encryptionProvider)
        : base(source)
    {
        SubscriptionKey = encryptionProvider.SafeDecrypt(source.Settings.SubscriptionKey);
        Region = source.Settings.Region;
        LogFilename = source.Settings.LogFilename;
        FilterProfanity = source.Settings.FilterProfanity;
        Diarization = source.Settings.Diarization;
    }

    public ConfiguredService<AzureSpeechServiceSettings> ToSettings(Guid serviceId, ILocalEncryptionProvider encryptionProvider)
    {
        return new ConfiguredService<AzureSpeechServiceSettings>
        {
            Id = serviceId,
            ServiceName = AzureSpeechServiceConstants.ServiceName,
            Label = string.IsNullOrWhiteSpace(Label) ? null : Label,
            Enabled = Enabled,
            Settings = new AzureSpeechServiceSettings
            {
                SubscriptionKey = encryptionProvider.Encrypt(SubscriptionKey),
                Region = Region,
                LogFilename = LogFilename,
                FilterProfanity = FilterProfanity,
                Diarization = Diarization,
            }
        };
    }
}
