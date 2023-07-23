namespace Voxta.Abstractions.Model;

public interface IReadOnlyChatSessionData
{
    string UserName { get; }
    CharacterCard Character { get; }
    string? Context { get; }
    string[]? Actions { get; }

    IReadOnlyList<ChatMessageData> GetMessages();
}

[Serializable]
public class ChatSessionData : IReadOnlyChatSessionData
{
    public Guid ChatId { get; init; }
    public required string UserName { get; init; }
    public required CharacterCard Character { get; init; }
    public string? Context { get; set; }
    public string[]? Actions { get; set; }
    public string[]? ThinkingSpeech { get; init; }

    public IReadOnlyList<ChatMessageData> GetMessages() => Messages.AsReadOnly();

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