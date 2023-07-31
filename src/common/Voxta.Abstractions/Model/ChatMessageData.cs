namespace Voxta.Abstractions.Model;

[Serializable]
public class ChatMessageData : TextData
{
    public Guid Id { get; init; }
    public required string User { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    public static ChatMessageData FromGen(string user, TextData gen)
    {
        return new ChatMessageData
        {
            Id = Guid.NewGuid(),
            User = user,
            Timestamp = DateTimeOffset.UtcNow,
            Text = gen.Text,
            Tokens = gen.Tokens,
        };
    }
}