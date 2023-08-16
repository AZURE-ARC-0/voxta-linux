using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Voxta.Abstractions.Model;

namespace Voxta.Server.ViewModels.ServiceSettings;

[Serializable]
public class ServiceSettingsWithParametersViewModel : ServiceSettingsViewModel
{
    public required bool UseDefaults { get; init; }
    public required string Parameters { get; init; }

    protected ServiceSettingsWithParametersViewModel()
    {
        
    }

    [SetsRequiredMembers]
    protected ServiceSettingsWithParametersViewModel(ConfiguredService service, SettingsBase source, object parameters, bool useDefaults)
    {
        Enabled = service.Enabled;
        Label = service.Label;
        Parameters = JsonSerializer.Serialize(parameters);
        UseDefaults = useDefaults;
    }

    protected TSettings? GetParameters<TSettings>()
        where TSettings : class, new()
    {
        return UseDefaults ? null : JsonSerializer.Deserialize<TSettings>(Parameters) ?? new TSettings();
    }
}
