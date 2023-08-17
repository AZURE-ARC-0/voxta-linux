namespace Voxta.Abstractions.Model;

[Serializable]
public class ChatSessionDataMemory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required TextData Text { get; set; }
}