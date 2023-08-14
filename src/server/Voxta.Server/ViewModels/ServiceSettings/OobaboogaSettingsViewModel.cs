using System.Diagnostics.CodeAnalysis;
using Voxta.Services.Oobabooga;

namespace Voxta.Server.ViewModels.ServiceSettings;

public class OobaboogaSettingsViewModel : LLMServiceSettingsViewModel
{
    public required string Uri { get; set; }

    public OobaboogaSettingsViewModel()
    {
    }

    [SetsRequiredMembers]
    public OobaboogaSettingsViewModel(OobaboogaSettings source)
        : base(source, source.Parameters ?? new OobaboogaParameters(), source.Parameters != null)
    {
        Uri = source.Uri;
    }

    public OobaboogaSettings ToSettings()
    {
        return new OobaboogaSettings
        {
            Enabled = Enabled,
            Uri = Uri.TrimCopyPasteArtefacts(),
            MaxContextTokens = MaxContextTokens,
            MaxMemoryTokens = MaxMemoryTokens,
            SummaryMaxTokens = SummaryMaxTokens,
            SummarizationDigestTokens = SummarizationDigestTokens,
            SummarizationTriggerTokens = SummarizationTriggerTokens,
            Parameters = GetParameters<OobaboogaParameters>(),
        };
    }
}