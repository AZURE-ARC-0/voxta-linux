using Voxta.Abstractions.Model;

namespace Voxta.Server.ViewModels.Settings;

public class SettingsViewModel
{
    public required ProfileSettings Profile { get; init; }
    public required SettingsServiceViewModel[] Services { get; init; }
}