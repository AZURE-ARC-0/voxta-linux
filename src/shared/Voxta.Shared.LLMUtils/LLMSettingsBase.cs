using Voxta.Abstractions.Repositories;

namespace Voxta.Shared.LLMUtils;

[Serializable]
public class LLMSettingsBase<TParameters> : SettingsBase
{
    public int MaxMemoryTokens { get; set; } = 400;
    public int MaxContextTokens { get; set; } = 1600;
    public TParameters? Parameters { get; set; }
}