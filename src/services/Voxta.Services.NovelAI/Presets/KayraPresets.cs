namespace Voxta.Services.NovelAI.Presets;

public static class KayraPresets
{
    public static NovelAIParameters FreshCoffee()
    {
	    return new NovelAIParameters
	    {
		    Temperature = 0.8,
		    MaxLength = 80,
		    MinLength = 1,
		    TopK = 25,
		    TopP = 1,
		    TopA = null,
		    TypicalP = null,
		    TailFreeSampling = 0.925,
		    RepetitionPenalty = 1.9,
		    RepetitionPenaltyFrequency = 0.0025,
		    RepetitionPenaltyPresence = 0.001,
		    RepetitionPenaltyRange = 768,
		    RepetitionPenaltySlope = 1,
		    PhraseRepPen = "aggressive",
		    Order = new[] {
			    6,
			    0,
			    1,
			    2,
			    3
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
		    LogitBiasExp = new LogitBiasExp[]
		    {
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
		    },
	    };
    }
}
