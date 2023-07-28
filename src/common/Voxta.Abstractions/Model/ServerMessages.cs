using System.Text.Json.Serialization;

namespace Voxta.Abstractions.Model;

[Serializable]
[JsonDerivedType(typeof(ServerWelcomeMessage), typeDiscriminator: "welcome")]
[JsonDerivedType(typeof(CharacterLoadedMessage), typeDiscriminator: "characterLoaded")]
[JsonDerivedType(typeof(ServerReadyMessage), typeDiscriminator: "ready")]
[JsonDerivedType(typeof(ServerReplyMessage), typeDiscriminator: "reply")]
[JsonDerivedType(typeof(ServerSpeechRecognitionStartMessage), typeDiscriminator: "speechRecognitionStart")]
[JsonDerivedType(typeof(ServerSpeechRecognitionPartialMessage), typeDiscriminator: "speechRecognitionPartial")]
[JsonDerivedType(typeof(ServerSpeechRecognitionEndMessage), typeDiscriminator: "speechRecognitionEnd")]
[JsonDerivedType(typeof(ServerSpeechMessage), typeDiscriminator: "speech")]
[JsonDerivedType(typeof(ServerActionMessage), typeDiscriminator: "action")]
[JsonDerivedType(typeof(ServerErrorMessage), typeDiscriminator: "error")]
public abstract class ServerMessage
{
}

[Serializable]
public class ServerWelcomeMessage : ServerMessage
{
    public required CharactersListItem[] Characters { get; set; }
    
    [Serializable]
    public class CharactersListItem
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string? Description { get; set; }
        public required bool ReadOnly { get; init; }
        public required string Culture { get; set; }
        public required string[] Prerequisites { get; set; }
    }
}

[Serializable]
public class CharacterLoadedMessage : ServerMessage
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Personality { get; init; }
    public required string Scenario { get; init; }
    public required string FirstMessage { get; init; }
    public required string MessageExamples { get; init; }
    public string? SystemPrompt { get; init; }
    public string? PostHistoryInstructions { get; init; }
    
    public required string Culture { get; init; }
    public string? Prerequisites { get; init; }
    public required string TextGenService { get; init; }
    public required string TtsService { get; init; }
    public required string TtsVoice { get; init; }
    
    public bool EnableThinkingSpeech { get; init; }
}

[Serializable]
public class ServerReadyMessage : ServerMessage
{
    public required Guid ChatId { get; init; }
    public required string[] ThinkingSpeechUrls { get; init; }
}

[Serializable]
public class ServerReplyMessage : ServerMessage
{
    public required string Text { get; set; }
}

[Serializable]
public class ServerSpeechRecognitionStartMessage : ServerMessage
{
}

[Serializable]
public class ServerSpeechRecognitionPartialMessage : ServerMessage
{
    public required string Text { get; set; }
}

[Serializable]
public class ServerSpeechRecognitionEndMessage : ServerMessage
{
    public required string Text { get; set; }
}

[Serializable]
public class ServerSpeechMessage : ServerMessage
{
    public required string Url { get; set; }
}

[Serializable]
public class ServerActionMessage : ServerMessage
{
    public required string Value { get; set; }
}

[Serializable]
public class ServerErrorMessage : ServerMessage
{
    public required string Message { get; set; }
}