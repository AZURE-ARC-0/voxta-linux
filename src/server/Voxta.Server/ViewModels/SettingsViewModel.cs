using Voxta.Abstractions.Model;
using Voxta.Host.AspNetCore.WebSockets.Utils;

namespace Voxta.Server.ViewModels;

public class SettingsViewModel
{
    public required ProfileSettings Profile { get; init; }
    public required SettingsServiceViewModel[] Services { get; init; }
}

public class SettingsServiceViewModel
{
    public required string Name { get; init; }
    public required string Title { get; init; }
    public required string Help { get; init; }
    public required ServiceDiagnosticsResult[] Services { get; init; }
}

