using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using LiteDB;
using Voxta.Abstractions.Services;

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
    
    [MinLength(1)]
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool PauseSpeechRecognitionDuringPlayback { get; set; } = true;
    public ServicesList ActionInference { get; set; } = new();
    public ServicesList SpeechToText { get; set; } = new();
    public ServicesList TextToSpeech { get; set; } = new();
    public ServicesList TextGen { get; set; } = new();
    public ServicesList Summarization { get; set; } = new();


    public DateTimeOffset Created { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastConnected { get; set; } = DateTimeOffset.UtcNow;

    public bool EnsureService(ConfiguredService settings, ServiceDefinition serviceDefinition)
    {
        var modified = false;
        if (serviceDefinition.TTS) modified = modified || EnsureService(TextToSpeech, settings);
        if (serviceDefinition.STT) modified = modified || EnsureService(SpeechToText, settings);
        if (serviceDefinition.TextGen) modified = modified || EnsureService(TextGen, settings);
        if (serviceDefinition.ActionInference) modified = modified || EnsureService(ActionInference, settings);
        if (serviceDefinition.Summarization) modified = modified || EnsureService(Summarization, settings);
        return modified;
    }

    private bool EnsureService(ServicesList servicesList, ConfiguredService settings)
    {
        if (servicesList.Services.Any(x => x.ServiceId == settings.Id))
            return false;

        servicesList.Services = servicesList.Services.Concat(new[] { new ServiceLink { ServiceName = settings.ServiceName, ServiceId = settings.Id } }).ToArray();
        return true;
    }
}