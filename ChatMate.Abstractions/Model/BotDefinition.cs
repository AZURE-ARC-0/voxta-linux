namespace ChatMate.Abstractions.Model;

[Serializable]
public class BotDefinition
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Preamble { get; init; }
    public string? Postamble { get; init; }
    public string? Greeting { get; init; }
    public string[]? ThinkingSpeech { get; init; }
    public required Message[] SampleMessages { get; init; }
    public required ServicesMap Services { get; init; }

    [Serializable]
    public class Message
    {
        public required string User { get; init; }
        public required string Text { get; init; }
    }
    
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
}