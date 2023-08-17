using Voxta.Host.AspNetCore.WebSockets.Utils;

namespace Voxta.Server.ViewModels.Diagnostics;

[Serializable]
public class TestViewModel
{
    public required SettingsServiceViewModel[] Services { get; init; }
}

public class SettingsServiceViewModel
{
    public required string Name { get; init; }
    public required string Title { get; init; }
    public required ServiceDiagnosticsResult[] Services { get; init; }
}
