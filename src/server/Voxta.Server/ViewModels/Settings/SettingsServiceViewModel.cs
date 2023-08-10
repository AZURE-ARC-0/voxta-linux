using Voxta.Host.AspNetCore.WebSockets.Utils;

namespace Voxta.Server.ViewModels.Settings;

public class SettingsServiceViewModel
{
    public required string Name { get; init; }
    public required string Title { get; init; }
    public required string Help { get; init; }
    public required ServiceDiagnosticsResult[] Services { get; init; }
}