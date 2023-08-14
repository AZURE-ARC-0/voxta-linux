using Voxta.Abstractions.Repositories;

namespace Voxta.Shared.LLMUtils;

[Serializable]
public class LLMSettingsBase : SettingsBase
{
    public int MaxMemoryTokens { get; set; } = 400;
    public int SummarizationTriggerTokens { get; set; } = 1000;
    public int SummarizationDigestTokens { get; set; } = 500;
    public int SummaryMaxTokens { get; set; } = 200;
    public int MaxContextTokens { get; set; } = 1600;
}

[Serializable]
public class LLMSettingsBase<TParameters> : LLMSettingsBase
{
    public TParameters? Parameters { get; set; }
}