namespace Voxta.Abstractions.Model;

public class MemoryItemComparer : Comparer<MemoryItem>
{
    public static readonly IComparer<MemoryItem> Current = new MemoryItemComparer();

    public override int Compare(MemoryItem? x, MemoryItem? y)
    {
        return x?.Weight.CompareTo(y?.Weight) ?? 0;
    }
}