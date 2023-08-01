using LiteDB;

namespace Voxta.Abstractions.Model;

[Serializable]
public class ChatMessageData : TextData
{
    public required string ChatId { get; init; }
    [BsonId] public required string Id { get; set; }
    public required string User { get; init; }
    public required DateTimeOffset Timestamp { get; init; }

    #warning Remove
    public static ChatMessageData Fake(string user, string message)
    {
        return new ChatMessageData
        {
            ChatId = Guid.Empty.ToString(),
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
            ChatId = chatId.ToString(),
            Id = Guid.NewGuid().ToString(),
            User = user,
            Timestamp = DateTimeOffset.UtcNow,
            Text = gen.Text,
            Tokens = gen.Tokens,
        };
    }
}