using System.ComponentModel.DataAnnotations;

namespace Voxta.Server.ViewModels.Settings;

public class ProfileViewModel
{
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must be an adult")]
    public required bool IsAdult { get; init; }
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms and conditions")]
    public required bool AgreesToTerms { get; init; }
    
    [MinLength(1)]
    public required string Name { get; init; }
    public required string? Description { get; init; }
    
    public required bool PauseSpeechRecognitionDuringPlayback { get; init; }
    public required bool IgnorePrerequisites { get; init; }
}