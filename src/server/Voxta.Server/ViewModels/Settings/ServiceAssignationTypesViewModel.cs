using Voxta.Host.AspNetCore.WebSockets.Utils;

namespace Voxta.Server.ViewModels.Settings;

public class ServiceAssignationTypesViewModel
{
    public required string Name { get; init; }
    public required string Title { get; init; }
    public required string Help { get; init; }
    public required ServiceDiagnosticsResult[] Services { get; init; }
}