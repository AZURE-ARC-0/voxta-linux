#if(!WINDOWS)
using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Model;
using Voxta.Services.FFmpeg;
#endif

namespace Voxta.Server.ViewModels.ServiceSettings;

public class FFmpegSettingsViewModel : ServiceSettingsViewModel
{
    #if(!WINDOWS)
    public FFmpegSettingsViewModel()
    {
    }

    [SetsRequiredMembers]
    public FFmpegSettingsViewModel(ConfiguredService<FFmpegSettings> source)
        : base(source)
    {
    }

    public ConfiguredService<FFmpegSettings> ToSettings(Guid serviceId)
    {
        return new ConfiguredService<FFmpegSettings>
        {
            Id = serviceId,
            ServiceName = FFmpegConstants.ServiceName,
            Label = string.IsNullOrWhiteSpace(Label) ? null : Label,
            Enabled = Enabled,
            Settings = new FFmpegSettings()
        };
    }
    #endif
}