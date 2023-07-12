namespace ChatMate.Abstractions.Model;

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
    public string? Id { get; set; }
    public bool ReadOnly { get; set; }
    
    public required ServicesMap Services { get; init; }
    public CharacterOptions? Options { get; init; }
    
    [Serializable]
    public class ServicesMap
    {
        public required ServiceMap TextGen { get; init; }
        public required VoiceServiceMap SpeechGen { get; init; }
    }

    [Serializable]
    public class ServiceMap
    {
        public required string Service { get; init; }
    }
    
    [Serializable]
    public class VoiceServiceMap : ServiceMap
    {
        public required string Voice { get; init; }
    }
    
    [Serializable]
    public class CharacterOptions
    {
        public bool EnableThinkingSpeech { get; init; }
    }
}