using System.Text.Json.Serialization;

namespace Voxta.Abstractions.Model;

[Serializable]
[JsonDerivedType(typeof(ServerWelcomeMessage), typeDiscriminator: "welcome")]
[JsonDerivedType(typeof(ServerCharactersListLoadedMessage), typeDiscriminator: "charactersListLoaded")]
[JsonDerivedType(typeof(ServerCharacterLoadedMessage), typeDiscriminator: "characterLoaded")]
[JsonDerivedType(typeof(ServerChatsListLoadedMessage), typeDiscriminator: "chatsListLoaded")]
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
    public required string Username { get; set; }
}

[Serializable]
public class ServerCharactersListLoadedMessage : ServerMessage
{
    public required CharactersListItem[] Characters { get; set; }
    
    [Serializable]
    public class CharactersListItem
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required string? Description { get; set; }
        public required bool ReadOnly { get; init; }
        public required string Culture { get; set; }
        public required string[] Prerequisites { get; set; }
    }
}

[Serializable]
public class ServerChatsListLoadedMessage : ServerMessage
{
    public required ChatsListItem[] Chats { get; set; }
    
    [Serializable]
    public class ChatsListItem
    {
        public required Guid Id { get; init; }
    }
}

[Serializable]
public class ServerCharacterLoadedMessage : ServerMessage
{
    public required Character Character { get; init; }
}

[Serializable]
public class ServerReadyMessage : ServerMessage
{
    public required Guid ChatId { get; init; }
    public required string[] ThinkingSpeechUrls { get; init; }
    public required CharacterServicesMap Services { get; init; }
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
    public required string? Text { get; set; }
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
    public string Message { get; set; }
    public string? Details { get; set; }

    public ServerErrorMessage(string message)
    {
        Message = message;
    }
    
    public ServerErrorMessage(Exception exc)
    {
        Message = exc.Message;
        Details = exc.ToString();
    }
}