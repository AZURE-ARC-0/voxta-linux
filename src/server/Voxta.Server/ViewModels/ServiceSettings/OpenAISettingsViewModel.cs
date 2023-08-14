namespace Voxta.Server.ViewModels.ServiceSettings;

public class OpenAISettingsViewModel : LLMServiceSettingsViewModel
{
    public required string ApiKey { get; set; }
    public required string Model { get; set; }
}