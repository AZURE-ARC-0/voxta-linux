using LiteDB;

namespace Voxta.Abstractions.Model;

[Serializable]
public class Chat
{
    [BsonId] public required Guid Id { get; set; }
    public required Guid CharacterId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
