using System.Text.Json.Serialization;

namespace Voxta.Abstractions.Model;

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
    public string? Context { get; init; }
    public string[]? Actions { get; init; }
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
    
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Personality { get; init; }
    public required string Scenario { get; init; }
    public string? FirstMessage { get; init; }
    public string? MessageExamples { get; init; }
    public string? SystemPrompt { get; init; }
    public string? PostHistoryInstructions { get; init; }

    public string Culture { get; init; } = "en-US";
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
