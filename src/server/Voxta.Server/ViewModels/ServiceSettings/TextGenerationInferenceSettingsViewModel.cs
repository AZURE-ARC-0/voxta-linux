using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Model;
using Voxta.Services.TextGenerationInference;

namespace Voxta.Server.ViewModels.ServiceSettings;

[Serializable]
public class TextGenerationInferenceSettingsViewModel : RemoteLLMServiceSettingsViewModelBase<TextGenerationInferenceParameters>
{
    public TextGenerationInferenceSettingsViewModel()
    {
    }
    
    [SetsRequiredMembers]
    public TextGenerationInferenceSettingsViewModel(ConfiguredService<TextGenerationInferenceSettings> source)
        : base(source, source.Settings)
    {
        Uri = source.Settings.Uri;
    }

    public ConfiguredService<TextGenerationInferenceSettings> ToSettings(Guid serviceId)
    {
        return new ConfiguredService<TextGenerationInferenceSettings>
        {
            Id = serviceId,
            ServiceName = TextGenerationInferenceConstants.ServiceName,
            Label = Label,
            Enabled = Enabled,
            Settings = new TextGenerationInferenceSettings
            {
                Uri = Uri.TrimCopyPasteArtefacts(),
                PromptFormat = PromptFormat,
                MaxContextTokens = MaxContextTokens,
                MaxMemoryTokens = MaxMemoryTokens,
                SummaryMaxTokens = SummaryMaxTokens,
                SummarizationDigestTokens = SummarizationDigestTokens,
                SummarizationTriggerTokens = SummarizationTriggerTokens,
                Parameters = GetParameters<TextGenerationInferenceParameters>(),
            },
        };
    }
}
