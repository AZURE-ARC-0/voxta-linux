using System.Text.Json.Serialization;

namespace Voxta.Abstractions.Model;

[Serializable]
[JsonDerivedType(typeof(ClientLoadCharactersListMessage), typeDiscriminator: "loadCharactersList")]
[JsonDerivedType(typeof(ClientLoadChatsListMessage), typeDiscriminator: "loadChatsList")]
[JsonDerivedType(typeof(ClientLoadCharacterMessage), typeDiscriminator: "loadCharacter")]
[JsonDerivedType(typeof(ClientNewChatMessage), typeDiscriminator: "newChat")]
[JsonDerivedType(typeof(ClientResumeChatMessage), typeDiscriminator: "resumeChat")]
[JsonDerivedType(typeof(ClientStartChatMessage), typeDiscriminator: "startChat")]
[JsonDerivedType(typeof(ClientStopChatMessage), typeDiscriminator: "stopChat")]
[JsonDerivedType(typeof(ClientSendMessage), typeDiscriminator: "send")]
[JsonDerivedType(typeof(ClientSpeechPlaybackStartMessage), typeDiscriminator: "speechPlaybackStart")]
[JsonDerivedType(typeof(ClientSpeechPlaybackCompleteMessage), typeDiscriminator: "speechPlaybackComplete")]
public abstract class ClientMessage
{
}

[Serializable]
public class ClientSendMessage : ClientMessage
{
    public required string Text { get; init; }
    public string? Context { get; init; }
    public string[]? Actions { get; init; }
}

[Serializable]
public class ClientLoadCharactersListMessage : ClientMessage
{
}

[Serializable]
public class ClientLoadChatsListMessage : ClientMessage
{
    public required Guid CharacterId { get; init; }
}

[Serializable]
public class ClientLoadCharacterMessage : ClientMessage
{
    public required Guid CharacterId { get; init; }
}

[Serializable]
public abstract class ClientDoChatMessageBase : ClientMessage
{
    public string? AudioPath { get; init; }
    public bool UseServerSpeechRecognition { get; init; } = true;
    public bool UseActionInference { get; init; } = true;
    public string[] AcceptedAudioContentTypes { get; set; } = { "audio/x-wav", "audio/mpeg" };
}

[Serializable]
public class ClientNewChatMessage : ClientDoChatMessageBase
{
    public Guid CharacterId { get; init; }
}

[Serializable]
public class ClientResumeChatMessage : ClientDoChatMessageBase
{
    public required Guid ChatId { get; init; }
}

[Serializable]
public class ClientStartChatMessage : ClientDoChatMessageBase
{
    public Guid? ChatId { get; init; }
    public required Character Character { get; init; }
}

[Serializable]
public class ClientStopChatMessage : ClientMessage
{
}

[Serializable]
public class ClientSpeechPlaybackStartMessage : ClientMessage
{
    public double Duration { get; init; }
}

[Serializable]
public class ClientSpeechPlaybackCompleteMessage : ClientMessage
{
}
