using ChatMate.Abstractions.Repositories;

namespace ChatMate.Data.Yaml;

public class SettingsYamlFileRepository : YamlFileRepositoryBase, ISettingsRepository
{
    public Task<T?> GetAsync<T>(string key) where T : class
    {
        return DeserializeFileAsync<T>($"Data/Services/{key}.yaml");
    }

    public Task SaveAsync<T>(string key, T value) where T : class
    {
        return SerializeFileAsync($"Data/Services/{key}.yaml", value);
    }
}