namespace Voxta.Server.ViewModels.ServiceSettings;

public class ElevenLabsSettingsViewModel : ServiceSettingsViewModel
{
    public required string ApiKey { get; set; }
    public required string Model { get; set; }
    public required string ThinkingSpeech { get; set; }
}