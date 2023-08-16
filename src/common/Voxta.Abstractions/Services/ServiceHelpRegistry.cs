namespace Voxta.Abstractions.Services;

public interface IServiceDefinitionsRegistry
{
    void Add(ServiceDefinition serviceDefinition);
    ServiceDefinition Get(string serviceName);
    IEnumerable<ServiceDefinition> List();
}

public class ServiceDefinitionsRegistry : IServiceDefinitionsRegistry
{
    private readonly Dictionary<string, ServiceDefinition> _services = new();
    
    public void Add(ServiceDefinition serviceDefinition)
    {
        _services.Add(serviceDefinition.ServiceName, serviceDefinition);
    }

    public ServiceDefinition Get(string serviceName)
    {
        return (_services.TryGetValue(serviceName, out var serviceHelp))
            ? serviceHelp
            : new ServiceDefinition
            {
                ServiceName = serviceName,
                Label = serviceName + " (Unknown)",
                Summarization = false,
                ActionInference = false,
                TextGen = false,
                STT = false,
                TTS = false,
                SettingsType = null,
            };
    }

    public IEnumerable<ServiceDefinition> List()
    {
        return _services.Values;
    }
}

public class ServiceDefinition
{
    public required string ServiceName { get; init; }
    public required string Label { get; init; }
    
    public required bool TTS { get; init; }
    public required bool STT { get; init; }
    public required bool TextGen { get; init; }
    public required bool ActionInference { get; init; }
    public required bool Summarization { get; init; }
    public required Type? SettingsType { get; init; }
}