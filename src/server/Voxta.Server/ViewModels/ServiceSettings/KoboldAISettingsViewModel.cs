using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Model;
using Voxta.Services.KoboldAI;

namespace Voxta.Server.ViewModels.ServiceSettings;

[Serializable]
public class KoboldAISettingsViewModel : RemoteLLMServiceSettingsViewModelBase<KoboldAIParameters>
{
    public KoboldAISettingsViewModel()
    {
    }
    
    [SetsRequiredMembers]
    public KoboldAISettingsViewModel(ConfiguredService<KoboldAISettings> source)
        : base(source, source.Settings)
    {
        Uri = source.Settings.Uri;
    }

    public ConfiguredService<KoboldAISettings> ToSettings(Guid serviceId)
    {
        return new ConfiguredService<KoboldAISettings>
        {
            Id = serviceId,
            ServiceName = KoboldAIConstants.ServiceName,
            Label = Label,
            Enabled = Enabled,
            Settings = new KoboldAISettings
            {
                Uri = Uri.TrimCopyPasteArtefacts(),
                PromptFormat = PromptFormat,
                MaxContextTokens = MaxContextTokens,
                MaxMemoryTokens = MaxMemoryTokens,
                SummaryMaxTokens = SummaryMaxTokens,
                SummarizationDigestTokens = SummarizationDigestTokens,
                SummarizationTriggerTokens = SummarizationTriggerTokens,
                Parameters = GetParameters<KoboldAIParameters>(),
            }
        };
    }
}