namespace Voxta.Server.ViewModels;

public class KoboldAISettingsViewModel : ServiceSettingsViewModel
{
    public required string Uri { get; set; }
    public required int MaxContextTokens { get; init; }
}