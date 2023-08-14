using LiteDB;

namespace Voxta.Abstractions.Model;

[Serializable]
public class ChatMessageData : MessageData
{
    [BsonId] public required Guid Id { get; set; }
    public required Guid ChatId { get; init; }
    public string? User { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public Guid? SummarizedBy { get; set; }

    public static ChatMessageData FromText(Guid chatId, ChatSessionDataCharacter character, string message)
    {
        return FromText(chatId, ChatMessageRole.Assistant, character.Name, message);
    }
    
    public static ChatMessageData FromText(Guid chatId, CharacterCard character, string message)
    {
        return FromText(chatId, ChatMessageRole.Assistant, character.Name, message);
    }

    public static ChatMessageData FromText(Guid chatId, ProfileSettings profile, string message)
    {
        return FromText(chatId, ChatMessageRole.User, profile.Name, message);
    }

    public static ChatMessageData FromText(Guid chatId, ChatSessionDataUser user, string message)
    {
        return FromText(chatId, ChatMessageRole.User, user.Name, message);
    }

    public static ChatMessageData FromText(Guid chatId, ChatMessageRole role, string user, string message)
    {
        if (string.IsNullOrEmpty(message)) throw new ArgumentException("Cannot create empty message", nameof(message));
        
        return new ChatMessageData
        {
            ChatId = chatId,
            Id = Guid.NewGuid(),
            Role = role,
            User = user,
            Timestamp = DateTimeOffset.UtcNow,
            Value = message,
            Tokens = 0,
        };
    }

    public static ChatMessageData FromGen(Guid chatId, ChatSessionDataCharacter character, TextData gen)
    {
        return FromGen(chatId, ChatMessageRole.Assistant, character.Name, gen);
    }
    
    public static ChatMessageData FromGen(Guid chatId, CharacterCard character, TextData gen)
    {
        return FromGen(chatId, ChatMessageRole.Assistant, character.Name, gen);
    }

    public static ChatMessageData FromGen(Guid chatId, ProfileSettings profile, TextData gen)
    {
        return FromGen(chatId, ChatMessageRole.User, profile.Name, gen);
    }

    public static ChatMessageData FromGen(Guid chatId, ChatSessionDataUser user, TextData gen)
    {
        return FromGen(chatId, ChatMessageRole.User, user.Name, gen);
    }

    public static ChatMessageData FromGen(Guid chatId, ChatMessageRole role, string user, TextData gen)
    {
        if (string.IsNullOrEmpty(gen.Value)) throw new ArgumentException("Cannot create empty message", nameof(gen));
        
        return new ChatMessageData
        {
            ChatId = chatId,
            Id = Guid.NewGuid(),
            Role = role,
            User = user,
            Timestamp = DateTimeOffset.UtcNow,
            Value = gen.Value,
            Tokens = gen.Tokens,
        };
    }
}