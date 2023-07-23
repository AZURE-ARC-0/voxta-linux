namespace Voxta.Abstractions.Services;

#warning Call Dispose, maybe use DisposeAsync instead
public interface IService : IDisposable
{
    Task InitializeAsync(CancellationToken cancellationToken);
}