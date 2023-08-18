namespace Voxta.Abstractions.Services;

public class ServiceDefinition
{
    public required string ServiceName { get; init; }
    public required string Label { get; init; }
    
    public required ServiceDefinitionCategoryScore TTS { get; init; }
    public required ServiceDefinitionCategoryScore STT { get; init; }
    public required ServiceDefinitionCategoryScore TextGen { get; init; }
    public required ServiceDefinitionCategoryScore ActionInference { get; init; }
    public required ServiceDefinitionCategoryScore Summarization { get; init; }
    
    public required string[] Features { get; init; }
    
    public required bool Recommended { get; init; }
    public required string Notes { get; init; }
    
    public required Type? SettingsType { get; init; }
}

public enum ServiceDefinitionCategoryScore
{
    /// <summary>
    /// This service cannot do this category
    /// </summary>
    NotSupported = 0,
    
    /// <summary>
    /// Works, but generally should not be used unless another option is available
    /// </summary>
    Low = 1,
    
    /// <summary>
    /// Normal quality
    /// </summary>
    Medium = 2,
    
    /// <summary>
    /// Excellent option, always use if possible
    /// </summary>
    High = 3,
}

public static class ServiceDefinitionCategoryScoreExtensions
{
    public static bool IsSupported(this ServiceDefinitionCategoryScore score)
    {
        return score > ServiceDefinitionCategoryScore.NotSupported;
    }
}
