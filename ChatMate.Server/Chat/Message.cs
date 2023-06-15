using System.Text.Json.Serialization;

namespace ChatMate.Server;

[Serializable]
[JsonDerivedType(typeof(ClientSendMessage), typeDiscriminator: "send")]
public abstract class ClientMessage
{
}

[Serializable]
public class ClientSendMessage : ClientMessage
{
    public required string Text { get; init; }
}

[Serializable]
[JsonDerivedType(typeof(ServerReplyMessage), typeDiscriminator: "reply")]
[JsonDerivedType(typeof(ServerSpeechMessage), typeDiscriminator: "speech")]
[JsonDerivedType(typeof(ServerAnimationMessage), typeDiscriminator: "animation")]
public abstract class ServerMessage
{
}

[Serializable]
public class ServerReplyMessage : ServerMessage
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
