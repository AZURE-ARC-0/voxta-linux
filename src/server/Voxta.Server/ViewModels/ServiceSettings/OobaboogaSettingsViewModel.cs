using System.Diagnostics.CodeAnalysis;
using Voxta.Services.Oobabooga;

namespace Voxta.Server.ViewModels.ServiceSettings;

public class OobaboogaSettingsViewModel : RemoteLLMServiceSettingsViewModelBase<OobaboogaParameters>
{
    public OobaboogaSettingsViewModel()
    {
    }
    
    [SetsRequiredMembers]
    public OobaboogaSettingsViewModel(OobaboogaSettings source)
        : base(source)
    {
    }

    public OobaboogaSettings ToSettings()
    {
        return new OobaboogaSettings
        {
            Enabled = Enabled,
            Uri = Uri.TrimCopyPasteArtefacts(),
            PromptFormat = PromptFormat,
            MaxContextTokens = MaxContextTokens,
            MaxMemoryTokens = MaxMemoryTokens,
            SummaryMaxTokens = SummaryMaxTokens,
            SummarizationDigestTokens = SummarizationDigestTokens,
            SummarizationTriggerTokens = SummarizationTriggerTokens,
            Parameters = GetParameters<OobaboogaParameters>(),
        };
    }
}