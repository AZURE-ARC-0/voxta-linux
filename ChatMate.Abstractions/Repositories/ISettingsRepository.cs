namespace ChatMate.Abstractions.Repositories;

public interface ISettingsRepository
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SaveAsync<T>(string key, T value) where T : class;
}