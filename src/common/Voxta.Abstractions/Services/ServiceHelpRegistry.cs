namespace Voxta.Abstractions.Services;

public interface IServiceHelpRegistry
{
    void Add(ServiceHelp serviceHelp);
    ServiceHelp Get(string serviceName);
    IEnumerable<ServiceHelp> List();
}

public class ServiceHelpRegistry : IServiceHelpRegistry
{
    private readonly Dictionary<string, ServiceHelp> _services = new();
    
    public void Add(ServiceHelp serviceHelp)
    {
        _services.Add(serviceHelp.ServiceName, serviceHelp);
    }

    public ServiceHelp Get(string serviceName)
    {
        return (_services.TryGetValue(serviceName, out var serviceHelp))
            ? serviceHelp
            : new ServiceHelp
            {
                ServiceName = serviceName,
                Label = serviceName + " (Unknown)",
                Summarization = false,
                ActionInference = false,
                TextGen = false,
                STT = false,
                TTS = false
            };
    }

    public IEnumerable<ServiceHelp> List()
    {
        return _services.Values;
    }
}

public class ServiceHelp
{
    public required string ServiceName { get; init; }
    public required string Label { get; init; }
    
    public required bool TTS { get; init; }
    public required bool STT { get; init; }
    public required bool TextGen { get; init; }
    public required bool ActionInference { get; init; }
    public required bool Summarization { get; init; }
}