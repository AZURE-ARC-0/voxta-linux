namespace Voxta.Services.NovelAI;

public static class ClioPresets
{
    public static NovelAIParameters TalkerC()
    {
        return new NovelAIParameters
        {
            Temperature = 1.5,
            MaxLength = 80,
            MinLength = 1,
            TopK = 10,
            TopP = 0.75,
            TopA = 0.08,
            TypicalP = 0.975,
            TailFreeSampling = 0.967,
            RepetitionPenalty = 2.25,
            RepetitionPenaltyFrequency = 0,
            RepetitionPenaltyPresence = 0.005,
            RepetitionPenaltyRange = 8192,
            RepetitionPenaltySlope = 0.09,
            PhraseRepPen = "very_light",
            Order = new int[] { 1, 5, 0, 2, 3, 4 },
            RepetitionPenaltyWhitelist = new[]
            {
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
            },
            BadWordsIds = new[]
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
                }
            },
            LogitBiasExp = Array.Empty<LogitBiasExp>(),
        };
    }
}
