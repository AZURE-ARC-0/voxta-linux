using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Model;
using Voxta.Services.Vosk;

namespace Voxta.Server.ViewModels.ServiceSettings;

public class VoskSettingsViewModel : ServiceSettingsViewModel
{
    public required string Model { get; init; }
    public required string? ModelHash { get; init; }
    public required string IgnoredWords { get; init; }

    public VoskSettingsViewModel()
    {
    }

    [SetsRequiredMembers]
    public VoskSettingsViewModel(ConfiguredService<VoskSettings> source)
        : base(source)
    {
        Model = source.Settings.Model;
        ModelHash = source.Settings.ModelHash;
        IgnoredWords = string.Join(", ", source.Settings.IgnoredWords);
    }

    public ConfiguredService<VoskSettings> ToSettings(Guid serviceId)
    {
        return new ConfiguredService<VoskSettings>
        {
            Id = serviceId,
            ServiceName = VoskConstants.ServiceName,
            Label = string.IsNullOrWhiteSpace(Label) ? null : Label,
            Enabled = Enabled,
            Settings = new VoskSettings
            {
                Model = Model.TrimCopyPasteArtefacts(),
                ModelHash = ModelHash?.TrimCopyPasteArtefacts() ?? "",
                IgnoredWords = IgnoredWords.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            }
        };
    }
}