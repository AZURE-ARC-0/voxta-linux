using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Voxta.Services.KoboldAI;

[Serializable]
[SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
public class KoboldAIParameters
{   
    [JsonPropertyName("max_length")]
    public double MaxLength { get; init; } = 80;
    
    [JsonPropertyName("rep_pen")]
    public double RepPen { get; init; } = 1.08;
    
    [JsonPropertyName("rep_pen_range")]
    public double RepPenRange { get; init; } = 1024;
    
    [JsonPropertyName("rep_pen_slope")]
    public double RepPenSlope { get; init; } = 0.9;
    
    [JsonPropertyName("tfs")]
    public double Tfs { get; init; } = 0.9;
    
    [JsonPropertyName("temperature")]
    public double Temperature { get; init; } = 0.65;
    
    [JsonPropertyName("top_p")]
    public double TopP { get; init; } = 0.9;
    
    [JsonPropertyName("sampler_order")]
    public int[] SamplerOrder { get; init; } = { 6, 0, 1, 2, 3, 4, 5 };
}

[Serializable]
[SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
public class KoboldAIRequestBody : KoboldAIParameters
{
    [JsonPropertyName("use_story")]
    public bool UseStory { get; init; } = false;
    
    [JsonPropertyName("use_memory")]
    public bool UseMemory { get; init; } = false;
    
    [JsonPropertyName("use_authors_note")]
    public bool UseAuthorsNote { get; init; } = false;
    
    [JsonPropertyName("use_world_info")]
    public bool UseWorldInfo { get; init; } = false;

    [JsonPropertyName("stop_sequence")]
    public string[]? StopSequence { get; set; }

    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }
}
