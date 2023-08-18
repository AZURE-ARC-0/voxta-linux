using Voxta.Abstractions.Model;

namespace Voxta.Server.ViewModels.Characters;

public class CharacterViewModel
{
    public required Character Character { get; init; }
    public required bool PrerequisiteNSFW { get; init; }
    public required string? TextGen { get; init; }
    public required string? TextToSpeech { get; init; }
    public required string? Voice { get; init; }
    public IFormFile? AvatarUpload { get; init; }
}

public class CharacterViewModelWithOptions : CharacterViewModel
{
    public required VoiceInfo[] Voices { get; set; } = Array.Empty<VoiceInfo>();
    public required OptionViewModel[] TextGenServices { get; init; }
    public required OptionViewModel[] TextToSpeechServices { get; init; }
    public required OptionViewModel[] Cultures { get; init; }
    public required bool IsNew { get; set; }
}