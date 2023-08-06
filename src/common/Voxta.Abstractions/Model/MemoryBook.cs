using LiteDB;

namespace Voxta.Abstractions.Model;

[Serializable]
public class MemoryBook
{
    [BsonId] public required Guid Id { get; set; }
    public required Guid CharacterId { get; set; }
    public Guid? ChatId { get; set; }

    public List<MemoryItem> Items { get; set; } = new();
}