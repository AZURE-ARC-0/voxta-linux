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
    
    [SetsRequiredMembers]
    public ServiceLink(string serviceName, Guid serviceId)
    {
        ServiceName = serviceName;
        ServiceId = serviceId;
    }
    
    [SetsRequiredMembers]
    public ServiceLink(ConfiguredService service)
    {
        ServiceName = service.ServiceName;
        ServiceId = service.Id;
    }

    public override string ToString()
    {
        return ServiceId != null ? $"{ServiceName} ({ServiceId})" : ServiceName;
    }
}