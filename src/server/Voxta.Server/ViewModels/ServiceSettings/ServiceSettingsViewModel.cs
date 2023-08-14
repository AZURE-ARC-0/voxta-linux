using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Voxta.Abstractions.Repositories;

namespace Voxta.Server.ViewModels.ServiceSettings;

[Serializable]
public class ServiceSettingsViewModel
{
    public required bool Enabled { get; init; }
    public required bool UseDefaults { get; init; }
    public required string Parameters { get; init; }

    protected ServiceSettingsViewModel()
    {
        
    }

    [SetsRequiredMembers]
    protected ServiceSettingsViewModel(SettingsBase source, object parameters, bool useDefaults)
    {
        Enabled = source.Enabled;
        Parameters = JsonSerializer.Serialize(parameters);
        UseDefaults = useDefaults;
    }

    protected TSettings? GetParameters<TSettings>()
        where TSettings : class, new()
    {
        return UseDefaults ? null : JsonSerializer.Deserialize<TSettings>(Parameters) ?? new TSettings();
    }
}
