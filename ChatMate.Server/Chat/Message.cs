using System.Text.Json.Serialization;

namespace ChatMate.Server;

[Serializable]
[JsonDerivedType(typeof(ClientSelectBotMessage), typeDiscriminator: "selectBot")]
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
public class ClientSelectBotMessage : ClientMessage
{
    public required string BotId { get; init; }
}

[Serializable]
[JsonDerivedType(typeof(ServerBotsListMessage), typeDiscriminator: "bots")]
[JsonDerivedType(typeof(ServerReplyMessage), typeDiscriminator: "reply")]
[JsonDerivedType(typeof(ServerSpeechMessage), typeDiscriminator: "speech")]
[JsonDerivedType(typeof(ServerAnimationMessage), typeDiscriminator: "animation")]
public abstract class ServerMessage
{
}

[Serializable]
public class ServerBotsListMessage : ServerMessage
{
    public required Bot[] Bots { get; set; }
    
    public class Bot
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
    }
}

[Serializable]
public class ServerReplyMessage : ServerMessage
{
    public required string Text { get; set; }
    public required string SpeechUrl { get; set; }
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
