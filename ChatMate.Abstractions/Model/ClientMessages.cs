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
    public bool UseServerSpeechRecognition { get; init; }
}

[Serializable]
public class ClientStopChatMessage : ClientMessage
{
}

[Serializable]
public class ClientListenMessage : ClientMessage
{
}
