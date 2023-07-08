namespace ChatMate.Abstractions.Model;

public interface IReadOnlyChatSessionData
{
    public string UserName { get; }
    public string BotName { get; }
    public TextData Preamble { get; }
    public TextData? Postamble { get; }
    public TextData? Greeting { get; }

    public IReadOnlyList<ChatMessageData> GetSampleMessages();
    public IReadOnlyList<ChatMessageData> GetMessages();
}

[Serializable]
public class ChatSessionData : IReadOnlyChatSessionData
{
    public Guid ChatId { get; init; }
    public required string UserName { get; init; }
    public required string BotName { get; init; }
    public required TextData Preamble { get; init; }
    public TextData? Postamble { get; init; }
    public TextData? Greeting { get; set; }
    public string[]? ThinkingSpeech { get; init; }

    public IReadOnlyList<ChatMessageData> GetSampleMessages() => Messages.AsReadOnly();
    public IReadOnlyList<ChatMessageData> GetMessages() => Messages.AsReadOnly();

    public List<ChatMessageData> SampleMessages { get; } = new();
    public List<ChatMessageData> Messages { get; } = new();
    
    public string? AudioPath { get; init; }
    public string? TtsVoice { get; set; }

    public string GetMessagesAsString()
    {
        return string.Join("\n", Messages.Select(m => $"{m.User}: {m.Text}"));
    }
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

    public static ChatMessageData FromGen(string user, TextData gen)
    {
        return new ChatMessageData
        {
            Id = Guid.NewGuid(),
            User = user,
            Timestamp = DateTimeOffset.UtcNow,
            Text = gen.Text,
            Tokens = gen.Tokens,
        };
    }
}