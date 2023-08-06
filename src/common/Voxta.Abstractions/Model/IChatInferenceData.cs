namespace Voxta.Abstractions.Model;

public interface IChatInferenceData
{
    string UserName { get; }
    CharacterCardExtended Character { get; }
    string? Context { get; }
    string[]? Actions { get; }

    IReadOnlyList<ChatMessageData> GetMessages();
    IReadOnlyList<MemoryItem> GetMemories();
}