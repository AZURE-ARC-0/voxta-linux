using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.System;
using Voxta.Services.NovelAI;
using Voxta.Services.NovelAI.Presets;

namespace Voxta.Server.ViewModels.ServiceSettings;

[Serializable]
public class NovelAISettingsViewModel : LLMServiceSettingsViewModel
{
    public required string Token { get; set; }
    public required string Model { get; set; }
    public required string ThinkingSpeech { get; set; }
    
    public NovelAISettingsViewModel()
    {
    }

    [SetsRequiredMembers]
    public NovelAISettingsViewModel(ConfiguredService<NovelAISettings> source, ILocalEncryptionProvider encryptionProvider)
        : base(source, source.Settings, source.Settings.Parameters ?? NovelAIPresets.DefaultForModel(source.Settings.Model), source.Settings.Parameters == null)
    {
        Token = encryptionProvider.SafeDecrypt(source.Settings.Token);
        Model = source.Settings.Model;
        ThinkingSpeech = string.Join('\n', source.Settings.ThinkingSpeech);
    }

    public ConfiguredService<NovelAISettings> ToSettings(Guid serviceId, ILocalEncryptionProvider encryptionProvider)
    {
        return new ConfiguredService<NovelAISettings>
        {
            Id = serviceId,
            ServiceName = NovelAIConstants.ServiceName,
            Label = Label,
            Enabled = Enabled,
            Settings = new NovelAISettings
            {
                Token = string.IsNullOrEmpty(Token) ? "" : encryptionProvider.Encrypt(Token.TrimCopyPasteArtefacts()),
                Model = Model.TrimCopyPasteArtefacts(),
                MaxContextTokens = MaxContextTokens,
                MaxMemoryTokens = MaxMemoryTokens,
                SummaryMaxTokens = SummaryMaxTokens,
                SummarizationDigestTokens = SummarizationDigestTokens,
                SummarizationTriggerTokens = SummarizationTriggerTokens,
                Parameters = GetParameters<NovelAIParameters>(),
                ThinkingSpeech = ThinkingSpeech.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            }
        };
    }
}