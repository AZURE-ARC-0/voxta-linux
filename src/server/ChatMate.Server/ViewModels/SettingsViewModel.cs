using ChatMate.Abstractions.Model;
using ChatMate.Services.KoboldAI;
using ChatMate.Services.ElevenLabs;
using ChatMate.Services.NovelAI;
using ChatMate.Services.Oobabooga;
using ChatMate.Services.OpenAI;

namespace ChatMate.Server.ViewModels;

public class SettingsViewModel
{
    public required OpenAISettings OpenAI { get; set; }
    public required NovelAISettings NovelAI { get; set; }
    public required KoboldAISettings KoboldAI { get; set; }
    public required OobaboogaSettings Oobabooga { get; set; }
    public required ElevenLabsSettings ElevenLabs { get; set; }
    public required ProfileSettings Profile { get; set; }
}