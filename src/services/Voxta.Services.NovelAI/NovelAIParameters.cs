using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Voxta.Services.NovelAI;

[Serializable]
[SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
public class NovelAIParameters
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 1.5;

    [JsonPropertyName("max_length")]
    public int MaxLength { get; set; } = 80;

    [JsonPropertyName("min_length")]
    public int MinLength { get; set; } = 1;

    [JsonPropertyName("top_k")]
    public double TopK { get; set; } = 10;

    [JsonPropertyName("top_p")]
    public double TopP { get; set; } = 0.75;

    [JsonPropertyName("top_a")]
    public double TopA { get; set; } = 0.08;

    [JsonPropertyName("typical_p")]
    public double TypicalP { get; set; } = 0.975;

    [JsonPropertyName("tail_free_sampling")]
    public double TailFreeSampling { get; set; } = 0.967;

    [JsonPropertyName("repetition_penalty")]
    public double RepetitionPenalty { get; set; } = 2.25;

    [JsonPropertyName("repetition_penalty_frequency")]
    public double RepetitionPenaltyFrequency { get; set; } = 0;

    [JsonPropertyName("repetition_penalty_presence")]
    public double RepetitionPenaltyPresence { get; set; } = 0.005;

    [JsonPropertyName("repetition_penalty_range")]
    public int RepetitionPenaltyRange { get; set; } = 8192;

    [JsonPropertyName("repetition_penalty_slope")]
    public double RepetitionPenaltySlope { get; set; } = 0.09;

    [JsonPropertyName("phrase_rep_pen")]
    public string PhraseRepPen { get; set; } = "very_light";

    [JsonPropertyName("order")]
    public int[] Order { get; set; } = { 1, 5, 0, 2, 3, 4 };

    [JsonPropertyName("repetition_penalty_whitelist")]
    public int[] RepetitionPenaltyWhitelist { get; set; } = {
        49256,
        49264,
        49231,
        49287,
        85,
        380,
        49216,
        49211,
        49215,
        49220,
        372,
        335,
        49223,
        49255,
        49399,
        49262,
        336,
        333,
        432,
        363,
        468,
        492,
        745,
        401,
        426,
        623,
        794,
        1096,
        2919,
        2072,
        7379,
        1259,
        2110,
        620,
        526,
        487,
        16562,
        603,
        805,
        761,
        2681,
        942,
        8917,
        653,
        3513,
        506,
        5301,
        562,
        5010,
        614,
        10942,
        539,
        2976,
        462,
        5189,
        567,
        2032,
        4,
        5,
        6,
        7,
        8,
        9,
        10,
        11,
        12,
        13,
        588,
        803,
        1040,
        49209
    };

    [JsonPropertyName("bad_words_ids")]
    public int[][] BadWordsIds { get; set; } = {

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
        }
    };
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