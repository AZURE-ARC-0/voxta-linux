using System.Text.Json.Serialization;

namespace ChatMate.Abstractions.Model;

[Serializable]
[JsonDerivedType(typeof(ClientLoadCharacterMessage), typeDiscriminator: "loadCharacter")]
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
}

[Serializable]
public class ClientLoadCharacterMessage : ClientMessage
{
    public required string CharacterId { get; init; }
}

[Serializable]
public class ClientStartChatMessage : ClientMessage
{
    public Guid? ChatId { get; init; }
    public string? AudioPath { get; init; }
    public bool UseServerSpeechRecognition { get; init; }
    
    public required string CharacterName { get; init; }
    public required string Preamble { get; init; }
    public string? Postamble { get; init; }
    public string? Greeting { get; init; }
    public string? SampleMessages { get; init; }
    
    public required string TextGenService { get; init; }
    public string? TtsService { get; init; }
    public string? TtsVoice { get; init; }
    public string[] AcceptedAudioContentTypes { get; set; } = { "audio/x-wav", "audio/mpeg" };
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
