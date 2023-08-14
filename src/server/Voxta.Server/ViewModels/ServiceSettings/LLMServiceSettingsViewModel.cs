namespace Voxta.Server.ViewModels.ServiceSettings;

public class LLMServiceSettingsViewModel : ServiceSettingsViewModel
{
    public required int MaxContextTokens { get; init; }
    public required int MaxMemoryTokens { get; init; }
}