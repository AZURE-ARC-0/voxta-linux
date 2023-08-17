using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Model;

namespace Voxta.Server.ViewModels.ServiceSettings;

[Serializable]
public class ServiceSettingsViewModel
{
    public ServiceSettingsViewModel()
    {
    }
    
    [SetsRequiredMembers]
    protected ServiceSettingsViewModel(ConfiguredService service)
    {
        Id = service.Id;
        Label = string.IsNullOrWhiteSpace(service.Label) ? null : service.Label;
        Enabled = service.Enabled;
    }
    
    public bool StayOnPage { get; init; }

    public Guid Id { get; init; }
    public required string? Label { get; init; }
    public required bool Enabled { get; init; }
}