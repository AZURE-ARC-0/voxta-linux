namespace Voxta.Server.ViewModels.ServiceSettings;

public class TextGenerationInferenceSettingsViewModel : LLMServiceSettingsViewModel
{
    public required string Uri { get; set; }
}