using System.ComponentModel.DataAnnotations;
using LiteDB;

namespace Voxta.Abstractions.Model;

[Serializable]
public class ProfileSettings
{
    public static readonly string SharedId = Guid.Empty.ToString();
    
    [BsonId] public string Id { get; init; } = SharedId;
    
    [MinLength(1)]
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool PauseSpeechRecognitionDuringPlayback { get; init; } = true;
    public required ProfileServicesMap Services { get; init; }
    
    [Serializable]
    public class ProfileServicesMap
    {
        public required ServiceMap ActionInference { get; init; }
        public required SpeechToTextServiceMap SpeechToText { get; init; }
    }
}