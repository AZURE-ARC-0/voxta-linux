using ChatMate.Abstractions.Model;
using ChatMate.Services.NovelAI;
using ChatMate.Services.OpenAI;

namespace ChatMate.Server.ViewModels;

public class SettingsViewModel
{
    public required OpenAISettings OpenAI { get; set; }
    public required NovelAISettings NovelAI { get; set; }
    public required ProfileSettings Profile { get; set; }
}