using System.Diagnostics.CodeAnalysis;
using Voxta.Services.KoboldAI;

namespace Voxta.Server.ViewModels.ServiceSettings;

[Serializable]
public class KoboldAISettingsViewModel : RemoteLLMServiceSettingsViewModelBase<KoboldAIParameters>
{
    public KoboldAISettingsViewModel()
    {
    }
    
    [SetsRequiredMembers]
    public KoboldAISettingsViewModel(KoboldAISettings source)
        : base(source)
    {
        Uri = source.Uri;
    }

    public KoboldAISettings ToSettings()
    {
        return new KoboldAISettings
        {
            Enabled = Enabled,
            Uri = Uri.TrimCopyPasteArtefacts(),
            PromptFormat = PromptFormat,
            MaxContextTokens = MaxContextTokens,
            MaxMemoryTokens = MaxMemoryTokens,
            SummaryMaxTokens = SummaryMaxTokens,
            SummarizationDigestTokens = SummarizationDigestTokens,
            SummarizationTriggerTokens = SummarizationTriggerTokens,
            Parameters = GetParameters<KoboldAIParameters>(),
        };
    }
}