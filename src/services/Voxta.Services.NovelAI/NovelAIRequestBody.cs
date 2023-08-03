using System.Text.Json.Serialization;

namespace Voxta.Services.NovelAI;

[Serializable]
public class NovelAIRequestBody
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }
    
    [JsonPropertyName("input")]
    public required string Input { get; init; }

    [JsonPropertyName("parameters")]
    public required NovelAIRequestBodyParameters Parameters { get; init; }
}