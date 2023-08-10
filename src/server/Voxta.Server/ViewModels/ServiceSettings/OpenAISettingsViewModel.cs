namespace Voxta.Server.ViewModels.ServiceSettings;

public class OpenAISettingsViewModel : ServiceSettingsViewModel
{
    public required string ApiKey { get; set; }
    public required string Model { get; set; }
    public required int MaxContextTokens { get; init; }
    public required int MaxMemoryTokens { get; init; }
}