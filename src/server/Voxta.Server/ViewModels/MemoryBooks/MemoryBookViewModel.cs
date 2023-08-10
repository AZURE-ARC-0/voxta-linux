using Voxta.Abstractions.Model;

namespace Voxta.Server.ViewModels.MemoryBooks;

[Serializable]
public class MemoryBookViewModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<MemoryItemViewModel> Items { get; set; } = new();
}

[Serializable]
public class MemoryItemViewModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Keywords { get; set; }
    public required int Weight { get; set; }
    public required string Text { get; set; }

    public MemoryItem ToModel()
    {
        return new MemoryItem
        {
            Keywords = Keywords.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray(),
            Text = Text,
            Weight = Weight,
            Id = Id,
        };
    }

    public static MemoryItemViewModel Create(MemoryItem x)
    {
        return new MemoryItemViewModel
        {
            Keywords = string.Join(", ", x.Keywords),
            Text = x.Text,
            Weight = x.Weight,
            Id = x.Id,
        };
    }
}