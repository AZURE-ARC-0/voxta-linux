using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Voxta.Services.NovelAI;

[Serializable]
public class NovelAIParameters
{
    [JsonPropertyName("cfg")]
    public double? Cfg { get; set; }
    
    [JsonPropertyName("cfg_uc")]
    public string? CfgUc { get; set; }
    
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
    public double? TopA { get; set; }

    [JsonPropertyName("typical_p")]
    public double? TypicalP { get; set; }

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
    public string? PhraseRepPen { get; set; }

    [JsonPropertyName("order")]
    public int[] Order { get; set; } = Array.Empty<int>();

    [JsonPropertyName("repetition_penalty_whitelist")]
    public int[]? RepetitionPenaltyWhitelist { get; set; }

    [JsonPropertyName("bad_words_ids")]
    public int[][] BadWordsIds { get; set; } = new[]
    {
        new[]
        {
            3
        },
        new[]
        {
            49356
        },
        new[]
        {
            1431
        },
        new[]
        {
            31715
        },
        new[]
        {
            34387
        },
        new[]
        {
            20765
        },
        new[]
        {
            30702
        },
        new[]
        {
            10691
        },
        new[]
        {
            49333
        },
        new[]
        {
            1266
        },
        new[]
        {
            19438
        },
        new[]
        {
            43145
        },
        new[]
        {
            26523
        },
        new[]
        {
            41471
        },
        new[]
        {
            2936
        },
        new[]
        {
            85,
            85
        },
        new[]
        {
            49332
        },
        new[]
        {
            7286
        },
        new[]
        {
            1115
        },
    };

    [JsonPropertyName("logit_bias_exp")]
    public LogitBiasExp[]? LogitBiasExp { get; set; } = {

        // Voxta
        new()
        {
            Bias = -2,
            EnsureSequenceFinish = true,
            GenerateOnce = false,
            Sequence = new[] { 49263 } // (
        },
        new()
        {
            Bias = -2,
            EnsureSequenceFinish = true,
            GenerateOnce = false,
            Sequence = new[] { 49356 } // [
        },
        new()
        {
            Bias = -2,
            EnsureSequenceFinish = true,
            GenerateOnce = false,
            Sequence = new[] { 49399 } // *
        },
        new()
        {
            Bias = -2,
            EnsureSequenceFinish = true,
            GenerateOnce = false,
            Sequence = new[] { 49534 } // ~
        },
        new()
        {
            Bias = -2,
            EnsureSequenceFinish = true,
            GenerateOnce = false,
            Sequence = new[] { 49292, 5576 } // OOC
        },
        // NovelAI Build In
        new()
        {
            Bias = -0.08,
            EnsureSequenceFinish = false,
            GenerateOnce = false,
            Sequence = new[]
            {
                23
            }
        },
        new()
        {
            Bias = -0.08,
            EnsureSequenceFinish = false,
            GenerateOnce = false,
            Sequence = new[]
            {
                21
            }
        }
    };
}

[Serializable]
public class LogitBiasExp
{
    [JsonPropertyName("bias")]
    public double Bias { get; init; }

    [JsonPropertyName("ensure_sequence_finish")]
    public bool EnsureSequenceFinish { get; init; }

    [JsonPropertyName("generate_once")]
    public bool GenerateOnce { get; init; }

    [JsonPropertyName("sequence")]
    public required int[] Sequence { get; init; }
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
        // \n
        new[] { 85 }
    };
}