namespace Voxta.Abstractions.Services;

public interface IService : IDisposable
{
    string ServiceName { get; }
    string[] Features { get; }
    Task<bool> TryInitializeAsync(Guid serviceId, string[] prerequisites, string culture, bool dry,
        CancellationToken cancellationToken);
}