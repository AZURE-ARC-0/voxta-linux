namespace Voxta.Abstractions.Model;

public interface IChatInferenceData
{
    string UserName { get; }
    CharacterCard Character { get; }
    string? Context { get; }
    string[]? Actions { get; }

    IReadOnlyList<ChatMessageData> GetMessages();
}