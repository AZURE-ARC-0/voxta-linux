using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Model;
using Voxta.Services.Oobabooga;

namespace Voxta.Server.ViewModels.ServiceSettings;

public class OobaboogaSettingsViewModel : RemoteLLMServiceSettingsViewModelBase<OobaboogaParameters>
{
    public OobaboogaSettingsViewModel()
    {
    }
    
    [SetsRequiredMembers]
    public OobaboogaSettingsViewModel(ConfiguredService<OobaboogaSettings> source)
        : base(source, source.Settings)
    {
    }

    public ConfiguredService<OobaboogaSettings> ToSettings(Guid serviceId)
    {
        return new ConfiguredService<OobaboogaSettings>
        {
            Id = serviceId,
            ServiceName = OobaboogaConstants.ServiceName,
            Label = string.IsNullOrWhiteSpace(Label) ? null : Label,
            Enabled = Enabled,
            Settings = new OobaboogaSettings
            {
                Uri = Uri.TrimCopyPasteArtefacts(),
                PromptFormat = PromptFormat,
                MaxContextTokens = MaxContextTokens,
                MaxMemoryTokens = MaxMemoryTokens,
                SummaryMaxTokens = SummaryMaxTokens,
                SummarizationDigestTokens = SummarizationDigestTokens,
                SummarizationTriggerTokens = SummarizationTriggerTokens,
                Parameters = GetParameters<OobaboogaParameters>(),
            }
        };
    }
}