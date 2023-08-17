namespace Voxta.Abstractions.Services;

public interface IServiceDefinitionsRegistry
{
    void Add(ServiceDefinition serviceDefinition);
    ServiceDefinition Get(string serviceName);
    IEnumerable<ServiceDefinition> List();
}