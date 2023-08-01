using LiteDB;

namespace Voxta.Abstractions.Model;

[Serializable]
public class ChatMessageData : TextData
{
    public required Guid ChatId { get; init; }
    [BsonId] public required string Id { get; set; }
    public required string User { get; init; }
    public required DateTimeOffset Timestamp { get; init; }

    public static ChatMessageData FromText(Guid chatId, string user, string message)
    {
        return new ChatMessageData
        {
            ChatId = chatId,
            Id = Guid.NewGuid().ToString(),
            User = user,
            Timestamp = DateTimeOffset.UtcNow,
            Text = message,
            Tokens = 0,
        };
    }

    public static ChatMessageData FromGen(Guid chatId, string user, TextData gen)
    {
        return new ChatMessageData
        {
            ChatId = chatId,
            Id = Guid.NewGuid().ToString(),
            User = user,
            Timestamp = DateTimeOffset.UtcNow,
            Text = gen.Text,
            Tokens = gen.Tokens,
        };
    }
}