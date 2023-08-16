#if(!WINDOWS)
using Voxta.Services.FFmpeg;
#endif

namespace Voxta.Server.ViewModels.ServiceSettings;

public class FFmpegSettingsViewModel
{
    public FFmpegSettingsViewModel()
    {
    }

    #if(!WINDOWS)
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