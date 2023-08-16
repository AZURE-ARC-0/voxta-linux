using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.System;
using Voxta.Services.ElevenLabs;

namespace Voxta.Server.ViewModels.ServiceSettings;

public class ElevenLabsSettingsViewModel : ServiceSettingsWithParametersViewModel
{
    public required string ApiKey { get; set; }
    public required string Model { get; set; }
    public required string ThinkingSpeech { get; set; }

    public ElevenLabsSettingsViewModel()
    {
    }

    [SetsRequiredMembers]
    public ElevenLabsSettingsViewModel(ConfiguredService<ElevenLabsSettings> source, ILocalEncryptionProvider encryptionProvider)
        : base(source, source.Settings, source.Settings.Parameters ?? new ElevenLabsParameters(), source.Settings.Parameters != null)
    {
        ApiKey = encryptionProvider.SafeDecrypt(source.Settings.ApiKey);
        Model = source.Settings.Model;
        ThinkingSpeech = string.Join('\n', source.Settings.ThinkingSpeech);
    }

    public ConfiguredService<ElevenLabsSettings> ToSettings(Guid serviceId, ILocalEncryptionProvider encryptionProvider)
    {
        return new ConfiguredService<ElevenLabsSettings>
        {
            Id = serviceId,
            ServiceName = ElevenLabsConstants.ServiceName,
            Label = string.IsNullOrWhiteSpace(Label) ? null : Label,
            Enabled = Enabled,
            Settings = new ElevenLabsSettings
            {
                ApiKey = string.IsNullOrEmpty(ApiKey) ? "" : encryptionProvider.Encrypt(ApiKey.TrimCopyPasteArtefacts()),
                Model = Model.TrimCopyPasteArtefacts(),
                Parameters = GetParameters<ElevenLabsParameters>(),
                ThinkingSpeech = ThinkingSpeech.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            }
        };
    }
}