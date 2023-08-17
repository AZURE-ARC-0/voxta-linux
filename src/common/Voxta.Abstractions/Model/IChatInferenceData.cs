using System.Globalization;

namespace Voxta.Abstractions.Model;

public interface IChatSessionData
{
    Guid Id { get; }
    ChatSessionDataUser User { get; }
    ChatSessionDataCharacter Character { get; }
    CultureInfo CultureInfo { get; }
    TextData? Context { get; }
    string[]? Actions { get; }
    int TotalMessagesTokens { get; }
}

public interface IChatInferenceData : IChatSessionData
{
    ChatSessionDataReadToken GetReadToken();
}

public interface IChatEditableData : IChatInferenceData
{
    ChatSessionDataWriteToken GetWriteToken();
}
