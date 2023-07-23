using Voxta.Abstractions.Model;
using Voxta.Services.AzureSpeechService;
using Voxta.Services.KoboldAI;
using Voxta.Services.ElevenLabs;
using Voxta.Services.NovelAI;
using Voxta.Services.Oobabooga;
using Voxta.Services.OpenAI;

namespace Voxta.Server.ViewModels;

public class SettingsViewModel
{
    public required OpenAISettings OpenAI { get; set; }
    public required NovelAISettings NovelAI { get; set; }
    public required KoboldAISettings KoboldAI { get; set; }
    public required OobaboogaSettings Oobabooga { get; set; }
    public required ElevenLabsSettings ElevenLabs { get; set; }
    public required AzureSpeechServiceSettings AzureSpeechService { get; set; }
    public required ProfileSettings Profile { get; set; }
}