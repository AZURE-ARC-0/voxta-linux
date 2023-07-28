using Voxta.Abstractions.Model;

namespace Voxta.Server.ViewModels;

public class SettingsViewModel
{
    public required ProfileSettings Profile { get; init; }
    public required SettingsServiceViewModel[] Services { get; init; }
}