using ChatMate.Abstractions.Repositories;

namespace ChatMate.Data.Yaml;

public class SettingsYamlFileRepository : YamlFileRepositoryBase, ISettingsRepository
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        return await DeserializeFileAsync<T>($"Data/Services/{key}.yaml", cancellationToken);
    }

    public Task SaveAsync<T>(string key, T value) where T : class
    {
        return SerializeFileAsync($"Data/Services/{key}.yaml", value);
    }
}