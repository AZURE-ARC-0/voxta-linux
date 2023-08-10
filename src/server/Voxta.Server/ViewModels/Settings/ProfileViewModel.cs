namespace Voxta.Server.ViewModels.Settings;

public class ProfileViewModel
{
    public required string Name { get; init; }
    public required string Description { get; set; }
    public required bool PauseSpeechRecognitionDuringPlayback { get; set; }
}