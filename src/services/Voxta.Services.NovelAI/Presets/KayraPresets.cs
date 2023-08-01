namespace Voxta.Services.NovelAI.Presets;

public static class KayraPresets
{
    public static NovelAIParameters Carefree()
    {
	    return new NovelAIParameters
	    {
		    Temperature = 1.35,
		    MaxLength = 80,
		    MinLength = 1,
		    TopK = 12,
		    TopP = 0.85,
		    TopA = 0.1,
		    TypicalP = 0.975,
		    TailFreeSampling = 0.915,
		    RepetitionPenalty = 2.8,
		    RepetitionPenaltyFrequency = 0.02,
		    RepetitionPenaltyPresence = 0,
		    RepetitionPenaltyRange = 2048,
		    RepetitionPenaltySlope = 0.02,
		    PhraseRepPen = "aggressive",
		    Order = new[] {
			    2,
			    3,
			    0,
			    4,
			    1
		    },
		    RepetitionPenaltyWhitelist = new[]
		    {
			    49256,
			    49264,
			    49231,
			    49230,
			    49287,
			    85,
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
			    123,
			    124,
			    125,
			    126,
			    127,
			    128,
			    129,
			    130,
			    131,
			    132,
			    588,
			    803,
			    1040,
			    49209,
			    4,
			    5,
			    6,
			    7,
			    8,
			    9,
			    10,
			    11,
			    12
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
		    },
		    LogitBiasExp = new LogitBiasExp[]
		    {
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
		    },
	    };
    }
}
