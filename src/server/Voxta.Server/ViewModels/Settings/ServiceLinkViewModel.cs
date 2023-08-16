using Voxta.Abstractions.Services;

namespace Voxta.Server.ViewModels.Settings;

public class ServiceLinksViewModel
{
    public required string Type { get; set; }
    public required string Title { get; set; }
    public required string Help { get; set; }
    public required List<ServiceLinkViewModel> ServiceLinks { get; init; }
}

[Serializable]
public class ServiceLinkViewModel
{
    public required bool Enabled { get; init; }
    public required Guid? ServiceId { get; init; }
    public required string ServiceName { get; init; }
    public required ServiceDefinition ServiceDefinition { get; init; }

    public string ServiceLinkString => $"{ServiceName}:{ServiceId ?? Guid.Empty}";
}