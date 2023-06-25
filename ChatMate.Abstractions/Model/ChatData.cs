namespace ChatMate.Abstractions.Model;

public interface IReadOnlyChatData
{
    public string BotName { get; }
    public string UserName { get; }
    public TextData Preamble { get; }
    public TextData Postamble { get; }
    public TextData Greeting { get; }

    public IReadOnlyList<ChatMessageData> GetSampleMessages();
    public IReadOnlyList<ChatMessageData> GetMessages();
}

[Serializable]
public class ChatData : IReadOnlyChatData
{
    public Guid Id { get; init; }
    
    public required string BotName { get; init; }
    public string UserName { get; init; } = "User";

    public required TextData Preamble { get; init; }
    public required TextData Postamble { get; init; }
    public required TextData Greeting { get; init; }

    public IReadOnlyList<ChatMessageData> GetSampleMessages() => Messages.AsReadOnly();
    public IReadOnlyList<ChatMessageData> GetMessages() => Messages.AsReadOnly();

    public List<ChatMessageData> SampleMessages { get; } = new();
    public List<ChatMessageData> Messages { get; } = new();
}

[Serializable]
public class TextData
{
    public required string Text { get; set; }
    public int Tokens { get; set; }
    public bool HasValue => !string.IsNullOrEmpty(Text);
}

[Serializable]
public class ChatMessageData : TextData
{
    public Guid Id { get; init; }
    public required string User { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}