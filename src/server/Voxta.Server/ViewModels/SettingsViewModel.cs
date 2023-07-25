using Voxta.Abstractions.Model;
using Voxta.Host.AspNetCore.WebSockets.Utils;

namespace Voxta.Server.ViewModels;

public class SettingsViewModel
{
    public required ProfileSettings Profile { get; init; }
    public List<ServiceDiagnosticsResult>? Services { get; init; }
}

public class NovelAISettingsViewModel
{
    public required string Token { get; set; }
    public required string Model { get; set; }
    public required bool UseDefaults { get; set; }
    public required string Parameters { get; set; }
}