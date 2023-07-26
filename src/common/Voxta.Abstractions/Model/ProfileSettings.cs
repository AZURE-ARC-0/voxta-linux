using System.ComponentModel.DataAnnotations;
using LiteDB;

namespace Voxta.Abstractions.Model;

[Serializable]
public class ProfileSettings
{
    public static readonly string SharedId = Guid.Empty.ToString();
    
    [BsonId] public string Id { get; init; } = SharedId;
    
    [MinLength(1)]
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool PauseSpeechRecognitionDuringPlayback { get; set; } = true;
    public ServicesList ActionInference { get; set; } = new();
    public ServicesList SpeechToText { get; set; } = new();
    public ServicesList TextToSpeech { get; set; } = new();
    public ServicesList TextGen { get; set; } = new();
}

[Serializable]
public class ServicesList
{
    public static ServicesList For(string service)
    {
        return new ServicesList { Services = new[] { service } };
    }

    public string[] Services { get; init; } = Array.Empty<string>();
}
