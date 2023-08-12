using System.Globalization;

namespace Voxta.Abstractions.Model;

public interface IChatInferenceData
{
    ChatSessionDataUser User { get; }
    ChatSessionDataCharacter Character { get; }
    string Culture { get; }
    CultureInfo CultureInfo { get; }
    TextData? Context { get; }
    string[]? Actions { get; }

    IReadOnlyList<ChatMessageData> GetMessages();
    IReadOnlyList<ChatSessionDataMemory> GetMemories();
}