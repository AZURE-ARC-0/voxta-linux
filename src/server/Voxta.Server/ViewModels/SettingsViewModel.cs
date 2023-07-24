using Voxta.Abstractions.Model;
using Voxta.Host.AspNetCore.WebSockets.Utils;

namespace Voxta.Server.ViewModels;

public class SettingsViewModel
{
    public required ProfileSettings Profile { get; init; }
    public List<ServiceDiagnosticsResult>? Services { get; init; }
}