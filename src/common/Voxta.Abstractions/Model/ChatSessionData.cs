namespace Voxta.Abstractions.Model;

[Serializable]
public class ChatSessionData : IChatInferenceData
{
    public required Chat Chat { get; init; }
    public required string UserName { get; init; }
    public required CharacterCardExtended Character { get; init; }
    public string? Context { get; set; }
    public string[]? Actions { get; set; }
    public string[]? ThinkingSpeech { get; init; }

    public IReadOnlyList<ChatMessageData> GetMessages() => Messages.AsReadOnly();
    public List<ChatMessageData> Messages { get; } = new();

    public string? AudioPath { get; init; }
    
    public IReadOnlyList<MemoryItem> GetMemories() => Memories.AsReadOnly();
    public List<MemoryItem> Memories { get; init; } = new();

    public string GetMessagesAsString()
    {
        return string.Join("\n", Messages.Select(m => $"{m.User}: {m.Text}"));
    }
}