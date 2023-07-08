using System.ComponentModel.DataAnnotations;

namespace ChatMate.Abstractions.Model;

[Serializable]
public class ProfileSettings
{
    [MinLength(1)]
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool EnableSpeechRecognition { get; init; } = true;
    public bool PauseSpeechRecognitionDuringPlayback { get; init; } = true;
    public string? AnimationSelectionService { get; init; }
}