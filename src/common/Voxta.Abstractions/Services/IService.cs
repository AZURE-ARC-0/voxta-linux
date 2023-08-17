using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Services;

public interface IService : IAsyncDisposable
{
    ServiceSettingsRef SettingsRef { get; }
    string[] Features { get; }
    Task<bool> TryInitializeAsync(Guid serviceId, string[] prerequisites, string culture, bool dry, CancellationToken cancellationToken);
}

[Serializable]
public class ServiceSettingsRef
{
    public required string ServiceName { get; set; }
    public Guid? ServiceId { get; set; }

    public ServiceLink ToLink() =>
        new()
        {
            ServiceName = ServiceName,
            ServiceId = ServiceId
        };
}