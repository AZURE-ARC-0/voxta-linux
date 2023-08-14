using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Voxta.Services.OpenAI;

[Serializable]
public class OpenAIParameters
{
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 80;
    
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.8;
    
    [JsonPropertyName("top_p")]
    public double TopP { get; set; } = 1;
    
    [JsonPropertyName("presence_penalty")]
    [Range(-2, 2)]
    public double PresencePenalty { get; init; } = 0.5;
    
    [JsonPropertyName("frequency_penalty")]
    [Range(-2, 2)]
    public double FrequencyPenalty { get; init; } = 0.5;
    
    [JsonPropertyName("logit_bias")]
    public Dictionary<string, int>? LogitBias { get; set; }
}

[Serializable]
public class OpenAIRequestBody : OpenAIParameters
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "gpt-3.5-turbo";
    
    [JsonPropertyName("messages")]
    public List<OpenAIMessage>? Messages { get; set; }

    [JsonPropertyName("stop")]
    public string[] Stop { get; set; } = { "\n" };
}
