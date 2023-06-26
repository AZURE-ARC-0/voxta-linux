using System.Collections.Concurrent;
using ChatMate.Abstractions.Repositories;

namespace ChatMate.Data.Yaml;

public class SettingsYamlFileRepository : YamlFileRepositoryBase, ISettingsRepository
{
    private readonly ConcurrentDictionary<string, Task<object?>> _cache = new();

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var value = await _cache.GetOrAdd(key, _ => DeserializeFileAsync<T>($"Data/Services/{key}.yaml").ContinueWith(t => (object?)t.Result));
        return (T?)value;
    }

    public Task SaveAsync<T>(string key, T value) where T : class
    {
        _cache[key] = Task.FromResult<object?>(value);
        return SerializeFileAsync($"Data/Services/{key}.yaml", value);
    }
}