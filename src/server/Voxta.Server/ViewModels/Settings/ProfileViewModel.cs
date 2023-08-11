using System.ComponentModel.DataAnnotations;

namespace Voxta.Server.ViewModels.Settings;

public class ProfileViewModel
{
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must be an adult")]
    public required bool IsAdult { get; init; }
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms and conditions")]
    public required bool AgreesToTerms { get; init; }
    public required string Name { get; init; }
    public required string Description { get; set; }
    public required bool PauseSpeechRecognitionDuringPlayback { get; set; }
}