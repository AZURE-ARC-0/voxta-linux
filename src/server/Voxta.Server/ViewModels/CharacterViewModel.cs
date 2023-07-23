using Voxta.Abstractions.Model;

namespace Voxta.Server.ViewModels;

public class CharacterViewModel
{
    public required Character Character { get; init; }
}

public class CharacterViewModelWithOptions : CharacterViewModel
{
    public VoiceInfo[] Voices { get; set; } = Array.Empty<VoiceInfo>();
    public required string[] TextGenServices { get; init; }
    public required string[] TextToSpeechServices { get; init; }
    public bool IsNew { get; set; }
}