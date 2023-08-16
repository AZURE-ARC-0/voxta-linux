using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;

namespace Voxta.Server.ViewModels.Settings;

public class SettingsViewModel
{
    public required ProfileSettings? Profile { get; init; }
    public required ConfiguredServiceViewModel[] Services { get; init; }
    public required ServiceAssignationTypesViewModel[] ServiceTypes { get; init; }
}

public class ConfiguredServiceViewModel
{
    public required ConfiguredService Service { get; init; }
    public required ServiceDefinition Definition { get; set; }
}