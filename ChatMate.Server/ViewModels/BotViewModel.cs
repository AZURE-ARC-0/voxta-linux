using ChatMate.Abstractions.Model;

namespace ChatMate.Server.ViewModels;

public class BotViewModel
{
    public required BotDefinition Bot { get; init; }
}

public class BotViewModelWithOptions : BotViewModel
{
    public VoiceInfo[] Voices { get; set; } = Array.Empty<VoiceInfo>();
    public required string[] TextGenServices { get; init; }
    public required string[] TextToSpeechServices { get; init; }
    public bool IsNew { get; set; }
}