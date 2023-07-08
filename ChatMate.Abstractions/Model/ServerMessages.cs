using System.Text.Json.Serialization;

namespace ChatMate.Abstractions.Model;

[Serializable]
[JsonDerivedType(typeof(ServerWelcomeMessage), typeDiscriminator: "welcome")]
[JsonDerivedType(typeof(BotTemplateLoadedMessage), typeDiscriminator: "botTemplateLoaded")]
[JsonDerivedType(typeof(ServerReadyMessage), typeDiscriminator: "ready")]
[JsonDerivedType(typeof(ServerReplyMessage), typeDiscriminator: "reply")]
[JsonDerivedType(typeof(ServerSpeechRecognitionStartMessage), typeDiscriminator: "speechRecognitionStart")]
[JsonDerivedType(typeof(ServerSpeechRecognitionEndMessage), typeDiscriminator: "speechRecognitionEnd")]
[JsonDerivedType(typeof(ServerSpeechMessage), typeDiscriminator: "speech")]
[JsonDerivedType(typeof(ServerAnimationMessage), typeDiscriminator: "animation")]
[JsonDerivedType(typeof(ServerErrorMessage), typeDiscriminator: "error")]
public abstract class ServerMessage
{
}

[Serializable]
public class ServerWelcomeMessage : ServerMessage
{
    public required BotTemplate[] BotTemplates { get; set; }
    
    [Serializable]
    public class BotTemplate
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public string? Description { get; set; }
    }
}

[Serializable]
public class BotTemplateLoadedMessage : ServerMessage
{
    public required string BotName { get; init; }
    public required string Preamble { get; init; }
    public required string Postamble { get; init; }
    public required string Greeting { get; init; }
    public required string SampleMessages { get; init; }
    
    public required string TextGenService { get; init; }
    public required string TtsService { get; init; }
    public required string TtsVoice { get; init; }
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
public class ServerAnimationMessage : ServerMessage
{
    public required string Value { get; set; }
}

[Serializable]
public class ServerErrorMessage : ServerMessage
{
    public required string Message { get; set; }
}