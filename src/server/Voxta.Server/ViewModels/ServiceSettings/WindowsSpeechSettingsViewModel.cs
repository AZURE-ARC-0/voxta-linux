namespace Voxta.Server.ViewModels.ServiceSettings;

public class WindowsSpeechSettingsViewModel
{
    public required string Label { get; init; }
    public required bool Enabled { get; init; }
    public required double MinimumConfidence { get; init; }
}