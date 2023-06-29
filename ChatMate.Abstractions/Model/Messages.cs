using System.Text.Json.Serialization;

namespace ChatMate.Abstractions.Model;

[Serializable]
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
public class ClientStartChatMessage : ClientMessage
{
    public required string BotId { get; init; }
    public string? AudioPath { get; init; }
}

[Serializable]
public class ClientStopChatMessage : ClientMessage
{
}

[Serializable]
public class ClientListenMessage : ClientMessage
{
}

[Serializable]
[JsonDerivedType(typeof(ServerWelcomeMessage), typeDiscriminator: "welcome")]
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
    public required Bot[] Bots { get; set; }
    
    [Serializable]
    public class Bot
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public string? Description { get; set; }
    }
}

[Serializable]
public class ServerReadyMessage : ServerMessage
{
    public required Guid ChatId { get; init; }
    public required string BotId { get; init; }
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