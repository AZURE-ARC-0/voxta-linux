#if(WINDOWS)
using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Model;
using Voxta.Services.WindowsSpeech;
#endif

namespace Voxta.Server.ViewModels.ServiceSettings;

public class WindowsSpeechSettingsViewModel : ServiceSettingsViewModel
{
    public required double MinimumConfidence { get; init; }

    #if(WINDOWS)
    public WindowsSpeechSettingsViewModel()
    {
    }

    [SetsRequiredMembers]
    public WindowsSpeechSettingsViewModel(ConfiguredService<WindowsSpeechSettings> source)
        : base(source)
    {
        MinimumConfidence = source.Settings.MinimumConfidence;
    }

    public ConfiguredService<WindowsSpeechSettings> ToSettings(Guid serviceId)
    {
        return new ConfiguredService<WindowsSpeechSettings>
        {
            Id = serviceId,
            ServiceName = WindowsSpeechConstants.ServiceName,
            Label = string.IsNullOrWhiteSpace(Label) ? null : Label,
            Enabled = Enabled,
            Settings = new WindowsSpeechSettings
            {
                MinimumConfidence = MinimumConfidence,
            }
        };
    }
    #endif
}