using Voxta.Abstractions.Model;
using Voxta.Host.AspNetCore.WebSockets.Utils;

namespace Voxta.Server.ViewModels;

public class SettingsViewModel
{
    public required ProfileSettings Profile { get; init; }
    public required (string Title, ServiceDiagnosticsResult[] Services)[] Services { get; init; }
}

public class ServiceSettingsViewModel
{
    public required bool Enabled { get; set; }
    public required bool UseDefaults { get; set; }
    public required string Parameters { get; set; }
}

public class NovelAISettingsViewModel : ServiceSettingsViewModel
{
    public required string Token { get; set; }
    public required string Model { get; set; }
}