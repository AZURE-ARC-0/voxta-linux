using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Voxta.Services.NovelAI;

[Serializable]
public class NovelAIParameters
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("max_length")]
    public int MaxLength { get; set; }

    [JsonPropertyName("min_length")]
    public int MinLength { get; set; }

    [JsonPropertyName("top_k")]
    public double TopK { get; set; }

    [JsonPropertyName("top_p")]
    public double TopP { get; set; }

    [JsonPropertyName("top_a")]
    public double TopA { get; set; }

    [JsonPropertyName("typical_p")]
    public double TypicalP { get; set; }

    [JsonPropertyName("tail_free_sampling")]
    public double TailFreeSampling { get; set; }

    [JsonPropertyName("repetition_penalty")]
    public double RepetitionPenalty { get; set; }

    [JsonPropertyName("repetition_penalty_frequency")]
    public double RepetitionPenaltyFrequency { get; set; }

    [JsonPropertyName("repetition_penalty_presence")]
    public double RepetitionPenaltyPresence { get; set; }

    [JsonPropertyName("repetition_penalty_range")]
    public int RepetitionPenaltyRange { get; set; }

    [JsonPropertyName("repetition_penalty_slope")]
    public double RepetitionPenaltySlope { get; set; }

    [JsonPropertyName("phrase_rep_pen")]
    public required string PhraseRepPen { get; set; }

    [JsonPropertyName("order")]
    public required int[] Order { get; set; }

    [JsonPropertyName("repetition_penalty_whitelist")]
    public int[]? RepetitionPenaltyWhitelist { get; set; }

    [JsonPropertyName("bad_words_ids")]
    public int[][] BadWordsIds { get; set; } = Array.Empty<int[]>();

    [JsonPropertyName("logit_bias_exp")]
    public LogitBiasExp[]? LogitBiasExp { get; set; }
}

[Serializable]
public class LogitBiasExp
{
    [JsonPropertyName("bias")]
    public double Bias { get; set; }

    [JsonPropertyName("ensure_sequence_finish")]
    public bool EnsureSequenceFinish { get; set; }

    [JsonPropertyName("generate_once")]
    public bool GenerateOnce { get; set; }

    [JsonPropertyName("sequence")]
    public int[] Sequence { get; set; } = Array.Empty<int>();
}

[Serializable]
[SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
public class NovelAIRequestBodyParameters : NovelAIParameters
{
    [JsonPropertyName("prefix")]
    public string Prefix { get; set; } = "vanilla";
    
    [JsonPropertyName("generate_until_sentence")]
    public bool GenerateUntilSentence { get; set; } = true;
    
    [JsonPropertyName("use_cache")]
    public bool UseCache { get; set; } = false;

    [JsonPropertyName("use_string")]
    public bool UseString { get; set; } = true;

    [JsonPropertyName("return_full_text")]
    public bool ReturnFullText { get; set; } = false;

    [JsonPropertyName("stop_sequences")]
    public int[][] StopSequences { get; set; } = {
        // User:
        new[] { 21978, 49287 },
        // "
        new[] { 49264 },
        // \n
        new[] { 85 }
    };
}