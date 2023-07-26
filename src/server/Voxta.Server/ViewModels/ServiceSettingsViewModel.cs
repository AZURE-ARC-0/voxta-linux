namespace Voxta.Server.ViewModels;

public class ServiceSettingsViewModel
{
    public required bool Enabled { get; init; }
    public required bool UseDefaults { get; init; }
    public required string Parameters { get; init; }
}
