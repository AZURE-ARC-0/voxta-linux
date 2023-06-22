namespace ChatMate.Server;

[Serializable]
public class BotDefinition
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Preamble { get; init; }
    public required string Postamble { get; init; }
    public required Message[] Messages { get; init; }
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
        public required ServiceMap SpeechGen { get; init; }
        public required ServiceMap AnimSelect { get; init; }
    }

    [Serializable]
    public class ServiceMap
    {
        public required string Service { get; init; }
        public Dictionary<string, string> Settings { get; init; } = new Dictionary<string, string>();
    }
}