using System.ComponentModel.DataAnnotations;
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
    [Range(0.0001, 100)]
    public double Temperature { get; set; } = 0.65;
    
    [JsonPropertyName("typical_p")]
    [Range(0.0001, 0.9999)]
    public double TypicalP { get; init; } = 0.95;
    
    [JsonPropertyName("top_p")]
    public double TopP { get; init; } = 0.9;
    
    [JsonPropertyName("top_k")]
    public double TopK { get; init; } = 10;
    
    [JsonPropertyName("do_sample")]
    public bool DoSample { get; init; } = true;
}

[Serializable]
[SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
public class TextGenerationInferenceParametersBody : TextGenerationInferenceParameters
{
    [JsonPropertyName("details")]
    public bool Details { get; init; } = false;
    
    [JsonPropertyName("decoder_input_details")]
    public bool DecoderInputDetails { get; init; } = false;
    
    [JsonPropertyName("watermark")]
    public bool Watermark { get; init; } = false;
    
    [JsonPropertyName("stop")]
    public string[]? Stop { get; set; }
    
    [JsonPropertyName("seed")]
    public double? Seed { get; init; }
}

[Serializable]
[SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
public class TextGenerationInferenceRequestBody
{
    [JsonPropertyName("parameters")]
    public required TextGenerationInferenceParametersBody Parameters { get; init; }

    [JsonPropertyName("inputs")]
    public required string Inputs { get; set; }
}
