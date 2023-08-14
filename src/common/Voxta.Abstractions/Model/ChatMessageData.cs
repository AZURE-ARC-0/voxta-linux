using LiteDB;

namespace Voxta.Abstractions.Model;

[Serializable]
public class ChatMessageData : TextData
{
    [BsonId] public required Guid Id { get; set; }
    public required Guid ChatId { get; init; }
    public required string User { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public Guid? SummarizedBy { get; set; }

    public static ChatMessageData FromText(Guid chatId, string user, string message)
    {
        if (string.IsNullOrEmpty(message)) throw new ArgumentException("Cannot create empty message", nameof(message));
        
        return new ChatMessageData
        {
            ChatId = chatId,
            Id = Guid.NewGuid(),
            User = user,
            Timestamp = DateTimeOffset.UtcNow,
            Value = message,
            Tokens = 0,
        };
    }

    public static ChatMessageData FromGen(Guid chatId, string user, TextData gen)
    {
        if (string.IsNullOrEmpty(gen.Value)) throw new ArgumentException("Cannot create empty message", nameof(gen));
        
        return new ChatMessageData
        {
            ChatId = chatId,
            Id = Guid.NewGuid(),
            User = user,
            Timestamp = DateTimeOffset.UtcNow,
            Value = gen.Value,
            Tokens = gen.Tokens,
        };
    }
}