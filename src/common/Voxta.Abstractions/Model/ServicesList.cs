namespace Voxta.Abstractions.Model;

[Serializable]
public class ServicesList
{
    public Guid[] Services { get; set; } = Array.Empty<Guid>();

    public int Order(string name)
    {
        var index = Array.IndexOf(Services, name);
        if (index == -1) return int.MaxValue;
        return index;
    }
}