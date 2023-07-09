namespace ChatMate.Abstractions.Services;

public interface IService
{
    Task InitializeAsync(CancellationToken cancellationToken);
}