using LiteDB;

namespace Voxta.Abstractions.Model;

[Serializable]
public class Chat
{
    [BsonId] public required string Id { get; set; }
    public required Character Character { get; set; }
}