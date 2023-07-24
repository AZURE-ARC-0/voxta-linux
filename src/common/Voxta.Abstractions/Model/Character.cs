using System.ComponentModel.DataAnnotations;
using LiteDB;

namespace Voxta.Abstractions.Model;

/// <summary>
/// See https://github.com/malfoyslastname/character-card-spec-v2 
/// </summary>
[Serializable]
public class CharacterCard
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Personality { get; init; }
    public required string Scenario { get; init; }
    public string? FirstMessage { get; init; }
    public string? MessageExamples { get; init; }
    public string? SystemPrompt { get; init; }
    public string? PostHistoryInstructions { get; init; }
    
    public string? CreatorNotes { get; init; }
}

[Serializable]
public class Character : CharacterCard
{
    [BsonId] public required string Id { get; set; }
    public bool ReadOnly { get; set; }
    [RegularExpression(@"^[a-z]{2}-[A-Z]{2}$")]
    public string Culture { get; set; } = "en-US";
    
    public required CharacterServicesMap Services { get; init; }
    public CharacterOptions? Options { get; init; }

    [Serializable]
    public class CharacterServicesMap
    {
        public required ServiceMap TextGen { get; init; }
        public required VoiceServiceMap SpeechGen { get; init; }
    }
    
    [Serializable]
    public class CharacterOptions
    {
        public bool EnableThinkingSpeech { get; init; }
    }
}