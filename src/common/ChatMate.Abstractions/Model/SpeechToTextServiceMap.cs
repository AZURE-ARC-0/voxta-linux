namespace ChatMate.Abstractions.Model;

[Serializable]
public class SpeechToTextServiceMap : ServiceMap
{
    public string? Model { get; init; }
    public string? Hash { get; init; }
}