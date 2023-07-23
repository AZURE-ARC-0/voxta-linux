namespace Voxta.Abstractions.Services;

public interface IService
{
    Task InitializeAsync(CancellationToken cancellationToken);
}