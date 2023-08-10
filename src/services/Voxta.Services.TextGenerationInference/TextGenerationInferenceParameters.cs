using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Voxta.Services.TextGenerationInference;

[Serializable]
[SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
public class TextGenerationInferenceParameters
{   
    [JsonPropertyName("max_new_tokens")]
    public double MaxNewTokens { get; init; } = 80;
    
    [JsonPropertyName("repetition_penalty")]
    public double RepetitionPenalty { get; init; } = 1.08;
    
    [JsonPropertyName("temperature")]
    public double Temperature { get; init; } = 0.65;
    
    [JsonPropertyName("typical_p")]
    public double TypicalP { get; init; } = 0.95;
    
    [JsonPropertyName("top_p")]
    public double TopP { get; init; } = 0.95;
    
    [JsonPropertyName("top_k")]
    public double TopK { get; init; } = 10;
}

[Serializable]
[SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
public class TextGenerationInferenceRequestBody : TextGenerationInferenceParameters
{
    [JsonPropertyName("watermark")]
    public bool Watermark { get; init; } = false;
    
    [JsonPropertyName("stop_sequences")]
    public string[]? StopSequence { get; set; }

    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }
}
