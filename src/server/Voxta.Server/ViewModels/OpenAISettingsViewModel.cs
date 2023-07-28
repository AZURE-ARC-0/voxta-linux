namespace Voxta.Server.ViewModels;

public class OpenAISettingsViewModel : ServiceSettingsViewModel
{
    public required string ApiKey { get; set; }
    public required string Model { get; set; }
}