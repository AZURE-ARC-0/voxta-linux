namespace Voxta.Abstractions.Services;

public interface IService : IDisposable
{
    Task<bool> InitializeAsync(string[] prerequisites, string culture, CancellationToken cancellationToken);
}