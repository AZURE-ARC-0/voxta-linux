using System.Diagnostics.CodeAnalysis;
using Voxta.Services.KoboldAI;

namespace Voxta.Server.ViewModels.ServiceSettings;

[Serializable]
public class KoboldAISettingsViewModel : LLMServiceSettingsViewModel
{
    public required string Uri { get; set; }

    [SetsRequiredMembers]
    public KoboldAISettingsViewModel(KoboldAISettings source)
        : base(source, source.Parameters ?? new KoboldAIParameters(), source.Parameters != null)
    {
        Uri = source.Uri;
    }

    public KoboldAISettings ToSettings()
    {
        return new KoboldAISettings
        {
            Enabled = Enabled,
            Uri = Uri.TrimCopyPasteArtefacts(),
            MaxContextTokens = MaxContextTokens,
            MaxMemoryTokens = MaxMemoryTokens,
            SummaryMaxTokens = SummaryMaxTokens,
            SummarizationDigestTokens = SummarizationDigestTokens,
            SummarizationTriggerTokens = SummarizationTriggerTokens,
            Parameters = GetParameters<KoboldAIParameters>(),
        };
    }
}