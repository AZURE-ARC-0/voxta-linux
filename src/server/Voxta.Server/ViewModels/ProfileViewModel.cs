using Voxta.Abstractions.Model;

namespace Voxta.Server.ViewModels;

public class ProfileViewModel
{
    public required string Name { get; init; }
    public required string Description { get; set; }
    public required bool PauseSpeechRecognitionDuringPlayback { get; set; }
}