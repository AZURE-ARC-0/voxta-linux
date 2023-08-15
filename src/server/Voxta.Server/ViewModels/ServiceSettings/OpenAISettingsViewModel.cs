using System.Diagnostics.CodeAnalysis;
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
    public OpenAISettingsViewModel(OpenAISettings source, ILocalEncryptionProvider encryptionProvider)
        : base(source, source.Parameters ?? new OpenAIParameters(), source.Parameters != null)
    {
        ApiKey = encryptionProvider.SafeDecrypt(source.ApiKey);
        Model = source.Model;
    }

    public OpenAISettings ToSettings(ILocalEncryptionProvider encryptionProvider)
    {
        return new OpenAISettings
        {
            Enabled = Enabled,
            ApiKey = string.IsNullOrEmpty(ApiKey) ? "" : encryptionProvider.Encrypt(ApiKey.TrimCopyPasteArtefacts()),
            Model = Model.TrimCopyPasteArtefacts(),
            MaxContextTokens = MaxContextTokens,
            MaxMemoryTokens = MaxMemoryTokens,
            SummaryMaxTokens = SummaryMaxTokens,
            SummarizationDigestTokens = SummarizationDigestTokens,
            SummarizationTriggerTokens = SummarizationTriggerTokens,
            Parameters = GetParameters<OpenAIParameters>(),
        };
    }
}