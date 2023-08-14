using System.Diagnostics.CodeAnalysis;
using Voxta.Services.TextGenerationInference;

namespace Voxta.Server.ViewModels.ServiceSettings;

public class TextGenerationInferenceSettingsViewModel : LLMServiceSettingsViewModel
{
    public required string Uri { get; set; }

    [SetsRequiredMembers]
    public TextGenerationInferenceSettingsViewModel(TextGenerationInferenceSettings source)
        : base(source, source.Parameters ?? new TextGenerationInferenceParameters(), source.Parameters != null)
    {
        Uri = source.Uri;
    }

    public TextGenerationInferenceSettings ToSettings()
    {
        return new TextGenerationInferenceSettings
        {
            Enabled = Enabled,
            Uri = Uri.TrimCopyPasteArtefacts(),
            MaxContextTokens = MaxContextTokens,
            MaxMemoryTokens = MaxMemoryTokens,
            SummaryMaxTokens = SummaryMaxTokens,
            SummarizationDigestTokens = SummarizationDigestTokens,
            SummarizationTriggerTokens = SummarizationTriggerTokens,
            Parameters = GetParameters<TextGenerationInferenceParameters>(),
        };
    }
}
