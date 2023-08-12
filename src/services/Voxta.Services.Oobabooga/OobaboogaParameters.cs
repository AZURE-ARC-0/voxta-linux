using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Voxta.Services.Oobabooga;

[Serializable]
[SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
public class OobaboogaParameters
{
    [JsonPropertyName("preset")]
    public string Preset { get; init; } = "None";
    
    [JsonPropertyName("min_length")]
    public int MinLength { get; init; } = 1;
    
    [JsonPropertyName("max_new_tokens")]
    public int MaxNewTokens { get; init; } = 80;
    
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;
    
    [JsonPropertyName("top_p")]
    public double TopP { get; init; } = 0.5;
    
    [JsonPropertyName("typical_p")]
    public double TypicalP { get; init; } = 1;
    
    [JsonPropertyName("tfs")]
    public double Tfs { get; init; } = 1;
    
    [JsonPropertyName("top_a")]
    public double TopA { get; init; } = 0;
    
    [JsonPropertyName("repetition_penalty")]
    public double RepetitionPenalty { get; init; } = 1.18;
    
    [JsonPropertyName("encoder_repetition_penalty")]
    public double EncoderRepetitionPenalty { get; init; } = 1;
    
    [JsonPropertyName("repetition_penalty_range")]
    public int RepetitionPenaltyRange { get; init; } = 0;
    
    [JsonPropertyName("top_k")]
    public double TopK { get; init; } = 40;
    
    [JsonPropertyName("no_repeat_ngram_size")]
    public double NoRepeatNgramSize { get; init; } = 0;
    
    [JsonPropertyName("penalty_alpha")]
    public double PenaltyAlpha { get; init; } = 0;
    
    [JsonPropertyName("length_penalty")]
    public int LengthPenalty { get; init; } = 1;
    
    [JsonPropertyName("seed")]
    public double Seed { get; init; } = -1;
}

[Serializable]
[SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
public class OobaboogaRequestBody : OobaboogaParameters
{
    [JsonPropertyName("do_sample")]
    public bool DoSample { get; init; } = true;
    
    [JsonPropertyName("early_stopping")]
    public bool EarlyStopping { get; init; } = true;
    
    [JsonPropertyName("num_beams")]
    public int NumBeams { get; init; } = 1;
    
    [JsonPropertyName("add_bos_token")]
    public bool AddBosToken { get; init; } = false;
    
    [JsonPropertyName("truncation_length")]
    public int TruncationLength { get; init; } = 2048;
    
    [JsonPropertyName("ban_eos_token")]
    public bool BanEosToken { get; init; } = false;
    
    [JsonPropertyName("skip_special_tokens")]
    public bool SkipSpecialTokens { get; init; } = true;

    [JsonPropertyName("stopping_strings")]
    public string[]? StoppingStrings { get; set; }

    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }
}
