using System.ComponentModel.DataAnnotations;
using LiteDB;

namespace Voxta.Abstractions.Model;

[Serializable]
public class Character : CharacterCard
{
    [BsonId] public required string Id { get; set; }
    public bool ReadOnly { get; set; }
    public string[]? Prerequisites { get; set; }
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