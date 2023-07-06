using ChatMate.Abstractions.Model;
using ChatMate.Services.KoboldAI;
using ChatMate.Services.NovelAI;
using ChatMate.Services.OpenAI;

namespace ChatMate.Server.ViewModels;

public class SettingsViewModel
{
    public required OpenAISettings OpenAI { get; set; }
    public required NovelAISettings NovelAI { get; set; }
    public required ProfileSettings Profile { get; set; }
    public required KoboldAISettings KoboldAI { get; set; }
}