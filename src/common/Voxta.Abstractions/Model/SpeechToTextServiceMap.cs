namespace Voxta.Abstractions.Model;

[Serializable]
public class SpeechToTextServiceMap : ServiceMap
{
    public string? Model { get; init; }
    #warning This should not be here
    public string? Hash { get; init; }
}