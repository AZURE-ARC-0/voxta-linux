namespace ChatMate.Abstractions.Repositories;

public interface ISettingsRepository
{
    Task<T> GetAsync<T>(string key);
    Task SaveAsync<T>(string key, T value);
}