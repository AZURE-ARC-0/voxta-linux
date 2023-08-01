using System.ComponentModel.DataAnnotations;
using LiteDB;

namespace Voxta.Abstractions.Model;

[Serializable]
public class CharacterCardExtended : CharacterCard
{
    public string[]? Prerequisites { get; set; }
    [RegularExpression(@"^[a-z]{2}-[A-Z]{2}$")]
    public string Culture { get; set; } = "en-US";
    
    public required CharacterServicesMap Services { get; init; }
    public CharacterOptions? Options { get; init; }
}

[Serializable]
public class Character : CharacterCardExtended
{
    [BsonId] public required string Id { get; set; }
    public bool ReadOnly { get; set; }
}
    

[Serializable]
public class CharacterServicesMap
{
    public ServiceMap? TextGen { get; init; }
    public VoiceServiceMap? SpeechGen { get; init; }
    public ServiceMap? ActionInference { get; init; }
    public ServiceMap? SpeechToText { get; init; }
}
    
[Serializable]
public class CharacterOptions
{
    public bool EnableThinkingSpeech { get; init; }
}