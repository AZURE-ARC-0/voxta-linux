using System.Text.Json.Serialization;

namespace ChatMate.Abstractions.Model;

[Serializable]
[JsonDerivedType(typeof(ClientLoadBotTemplateMessage), typeDiscriminator: "loadBotTemplate")]
[JsonDerivedType(typeof(ClientStartChatMessage), typeDiscriminator: "startChat")]
[JsonDerivedType(typeof(ClientStopChatMessage), typeDiscriminator: "stopChat")]
[JsonDerivedType(typeof(ClientSendMessage), typeDiscriminator: "send")]
[JsonDerivedType(typeof(ClientListenMessage), typeDiscriminator: "listen")]
public abstract class ClientMessage
{
}

[Serializable]
public class ClientSendMessage : ClientMessage
{
    public required string Text { get; init; }
}

[Serializable]
public class ClientLoadBotTemplateMessage : ClientMessage
{
    public required string BotTemplateId { get; init; }
}

[Serializable]
public class ClientStartChatMessage : ClientMessage
{
    public Guid? ChatId { get; init; }
    public string? AudioPath { get; init; }
    public bool UseServerSpeechRecognition { get; init; }
    
    public required string BotName { get; init; }
    public required string Preamble { get; init; }
    public string? Postamble { get; init; }
    public string? Greeting { get; init; }
    public string? SampleMessages { get; init; }
    
    public required string TextGenService { get; init; }
    public string? TtsService { get; init; }
    public string? TtsVoice { get; init; }
}

[Serializable]
public class ClientStopChatMessage : ClientMessage
{
}

[Serializable]
public class ClientListenMessage : ClientMessage
{
}
