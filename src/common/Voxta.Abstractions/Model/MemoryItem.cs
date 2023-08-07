namespace Voxta.Abstractions.Model;

[Serializable]
public class MemoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string[] Keywords { get; set; }
    public required int Weight { get; set; }
    public required string Text { get; set; }
}