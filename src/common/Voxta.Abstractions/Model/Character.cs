using System.ComponentModel.DataAnnotations;
using LiteDB;

namespace Voxta.Abstractions.Model;

[Serializable]
public class CharacterCardExtended : CharacterCard
{
    public string[]? Prerequisites { get; set; }
    [RegularExpression(@"^[a-z]{2}-[A-Z]{2}$")]
    public string Culture { get; set; } = "en-US";
    
    public required CharacterServicesMap Services { get; set; }
    public CharacterOptions? Options { get; init; }
}

[Serializable]
public class Character : CharacterCardExtended
{
    [BsonId] public required Guid Id { get; set; }
    public bool ReadOnly { get; set; }
}

[Serializable]
public class CharacterServicesMap
{
    public ServiceMap TextGen { get; init; } = new();
    public VoiceServiceMap SpeechGen { get; init; } = new();
    public ServiceMap ActionInference { get; init; } = new();
    public ServiceMap SpeechToText { get; init; } = new();
}
    
[Serializable]
public class CharacterOptions
{
    public bool EnableThinkingSpeech { get; init; }
}