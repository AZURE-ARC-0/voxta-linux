using System.Diagnostics.CodeAnalysis;
using LiteDB;

namespace Voxta.Abstractions.Model;

[Serializable]
public class ProfileSettings
{
    public static readonly string SharedId = Guid.Empty.ToString();

    public ProfileSettings()
    {
    }

    [SetsRequiredMembers]
    public ProfileSettings(string name, ServicesList textGen)
    {
        Name = name;
        TextGen = textGen;
    }

    [BsonId] public string Id { get; init; } = SharedId;
    
    public required string Name { get; set; }
    public string? Description { get; set; }
    
    public bool PauseSpeechRecognitionDuringPlayback { get; set; } = true;
    public bool IgnorePrerequisites { get; set; }
    public bool HideNSFW { get; set; }
    
    public ServicesList ActionInference { get; set; } = new();
    public ServicesList SpeechToText { get; set; } = new();
    public ServicesList TextToSpeech { get; set; } = new();
    public ServicesList TextGen { get; set; } = new();
    public ServicesList Summarization { get; set; } = new();


    public DateTimeOffset Created { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastConnected { get; set; } = DateTimeOffset.UtcNow;
}