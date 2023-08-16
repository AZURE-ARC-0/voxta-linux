using System.Diagnostics.CodeAnalysis;

namespace Voxta.Abstractions.Model;

[Serializable]
public class ServicesList
{
    public ServiceLink[] Services { get; set; } = Array.Empty<ServiceLink>();

    public int Order(string name)
    {
        var index = Array.IndexOf(Services, name);
        if (index == -1) return int.MaxValue;
        return index;
    }
}

[Serializable]
public class ServiceLink
{
    public required string ServiceName { get; set; }
    public Guid? ServiceId { get; set; }

    public ServiceLink()
    {
    }
    
    [SetsRequiredMembers]
    public ServiceLink(string serviceName)
    {
        ServiceName = serviceName;
    }

    public override string ToString()
    {
        return ServiceId != null ? $"{ServiceName} ({ServiceId})" : ServiceName;
    }
}