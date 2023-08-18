namespace Voxta.Abstractions.Services;

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
                Summarization = ServiceDefinitionCategoryScore.NotSupported,
                ActionInference = ServiceDefinitionCategoryScore.NotSupported,
                TextGen = ServiceDefinitionCategoryScore.NotSupported,
                STT = ServiceDefinitionCategoryScore.NotSupported,
                TTS = ServiceDefinitionCategoryScore.NotSupported,
                Features = Array.Empty<string>(),
                Recommended = false,
                Notes = "This service is not supported.",
                SettingsType = null,
            };
    }

    public IEnumerable<ServiceDefinition> List()
    {
        return _services.Values;
    }
}