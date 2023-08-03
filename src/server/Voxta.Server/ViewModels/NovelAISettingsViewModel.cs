namespace Voxta.Server.ViewModels;

public class NovelAISettingsViewModel : ServiceSettingsViewModel
{
    public required string Token { get; set; }
    public required string Model { get; set; }
    public required int MaxContextTokens { get; init; }
    public required string ThinkingSpeech { get; set; }
}