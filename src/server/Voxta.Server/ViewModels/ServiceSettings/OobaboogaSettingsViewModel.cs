namespace Voxta.Server.ViewModels.ServiceSettings;

public class OobaboogaSettingsViewModel : ServiceSettingsViewModel
{
    public required string Uri { get; set; }
    public required int MaxContextTokens { get; init; }
    public required int MaxMemoryTokens { get; init; }
}