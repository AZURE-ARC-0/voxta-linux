namespace Voxta.Abstractions.Model;

[Serializable]
public class ServicesList
{
    public static ServicesList For(string service)
    {
        return new ServicesList { Services = new[] { service } };
    }

    public string[] Services { get; set; } = Array.Empty<string>();

    public int Order(string name)
    {
        var index = Array.IndexOf(Services, name);
        if (index == -1) return int.MaxValue;
        return index;
    }
}