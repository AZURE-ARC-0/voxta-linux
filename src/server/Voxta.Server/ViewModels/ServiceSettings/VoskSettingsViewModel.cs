namespace Voxta.Server.ViewModels.ServiceSettings;

public class VoskSettingsViewModel
{
    public bool Enabled { get; init; }
    public required string Model { get; init; }
    public required string? ModelHash { get; init; }
    public required string IgnoredWords { get; init; }
}