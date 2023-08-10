using Voxta.Abstractions.Repositories;

namespace Voxta.Services.TextGenerationInference;

[Serializable]
public class TextGenerationInferenceSettings : SettingsBase
{
    public required string Uri { get; set; }
    public int MaxMemoryTokens { get; set; } = 400;
    public int MaxContextTokens { get; set; } = 1600;
    public TextGenerationInferenceParameters? Parameters { get; set; }
}