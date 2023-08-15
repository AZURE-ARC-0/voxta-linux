using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.System;
using Voxta.Services.OpenAI;

namespace Voxta.Server.ViewModels.ServiceSettings;

public class OpenAISettingsViewModel : LLMServiceSettingsViewModel
{
    public required string ApiKey { get; set; }
    public required string Model { get; set; }
    
    public OpenAISettingsViewModel()
    {
    }

    [SetsRequiredMembers]
    public OpenAISettingsViewModel(ConfiguredService<OpenAISettings> source, ILocalEncryptionProvider encryptionProvider)
        : base(source, source.Settings, source.Settings.Parameters ?? new OpenAIParameters(), source.Settings.Parameters == null)
    {
        ApiKey = encryptionProvider.SafeDecrypt(source.Settings.ApiKey);
        Model = source.Settings.Model;
    }

    public ConfiguredService<OpenAISettings> ToSettings(Guid serviceId, ILocalEncryptionProvider encryptionProvider)
    {
        return new ConfiguredService<OpenAISettings>
        {
            Id = serviceId,
            ServiceName = OpenAIConstants.ServiceName,
            Label = Label,
            Enabled = Enabled,
            Settings = new OpenAISettings
            {
                ApiKey = string.IsNullOrEmpty(ApiKey) ? "" : encryptionProvider.Encrypt(ApiKey.TrimCopyPasteArtefacts()),
                Model = Model.TrimCopyPasteArtefacts(),
                MaxContextTokens = MaxContextTokens,
                MaxMemoryTokens = MaxMemoryTokens,
                SummaryMaxTokens = SummaryMaxTokens,
                SummarizationDigestTokens = SummarizationDigestTokens,
                SummarizationTriggerTokens = SummarizationTriggerTokens,
                Parameters = GetParameters<OpenAIParameters>(),
            }
        };
    }
}