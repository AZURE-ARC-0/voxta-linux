using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Voxta.Abstractions.Model;

namespace Voxta.Services.ElevenLabs;

[Serializable]
public class ElevenLabsSettings : SettingsBase
{
    public required string ApiKey { get; set; }
    public string Model { get; set; } = "eleven_multilingual_v1";
    public string[] ThinkingSpeech { get; set; } = {
        "m",
        "uh",
        "..",
        "...",
        "mmh",
        "hum",
        "huh",
        "!!",
        "??",
        "o",
    };
    public ElevenLabsParameters? Parameters { get; set; }
}

[Serializable]
public class ElevenLabsParameters
{
    [JsonPropertyName("stability")]
    [Range(0, 1)]
    public double Stability { get; init; } = 0.35;
   
    [JsonPropertyName("similarity_boost")]
    [Range(0, 1)]
    public double SimilarityBoost { get; init; } = 0.75;
}