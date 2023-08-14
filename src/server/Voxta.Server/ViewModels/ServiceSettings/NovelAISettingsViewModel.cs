namespace Voxta.Server.ViewModels.ServiceSettings;

public class NovelAISettingsViewModel : LLMServiceSettingsViewModel
{
    public required string Token { get; set; }
    public required string Model { get; set; }
    public required string ThinkingSpeech { get; set; }
}