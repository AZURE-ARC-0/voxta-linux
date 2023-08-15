using System.Diagnostics.CodeAnalysis;
using Voxta.Services.TextGenerationInference;

namespace Voxta.Server.ViewModels.ServiceSettings;

[Serializable]
public class TextGenerationInferenceSettingsViewModel : RemoteLLMServiceSettingsViewModelBase<TextGenerationInferenceParameters>
{
    public TextGenerationInferenceSettingsViewModel()
    {
    }
    
    [SetsRequiredMembers]
    public TextGenerationInferenceSettingsViewModel(TextGenerationInferenceSettings source)
        : base(source)
    {
    }

    public TextGenerationInferenceSettings ToSettings()
    {
        return new TextGenerationInferenceSettings
        {
            Enabled = Enabled,
            Uri = Uri.TrimCopyPasteArtefacts(),
            PromptFormat = PromptFormat,
            MaxContextTokens = MaxContextTokens,
            MaxMemoryTokens = MaxMemoryTokens,
            SummaryMaxTokens = SummaryMaxTokens,
            SummarizationDigestTokens = SummarizationDigestTokens,
            SummarizationTriggerTokens = SummarizationTriggerTokens,
            Parameters = GetParameters<TextGenerationInferenceParameters>(),
        };
    }
}
