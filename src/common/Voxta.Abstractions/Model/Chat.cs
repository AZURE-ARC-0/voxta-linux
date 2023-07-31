using LiteDB;

namespace Voxta.Abstractions.Model;

[Serializable]
public class Chat
{
    [BsonId] public required string Id { get; set; }
    public required CharacterCard Character { get; init; }
    public List<ChatMessageData> Messages { get; } = new();
}