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

    public bool SyncWithTemplate(ServicesList template)
    {
        var textGenServicesToAdd = template.Services.Except(Services).ToArray();
        var textGenServicesToRemove = Services.Except(template.Services).ToArray();
        if (!textGenServicesToAdd.Any() && !textGenServicesToRemove.Any()) return false;
        
        var textGenServices = new List<string>(Services);
        textGenServices.AddRange(textGenServicesToAdd);
        textGenServices.RemoveAll(x => textGenServicesToRemove.Contains(x));
        return true;
    }
}