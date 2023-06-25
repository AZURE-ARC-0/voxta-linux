using ChatMate.Abstractions.Repositories;

namespace ChatMate.Data.Yaml;

public class SettingsYamlFileRepository : YamlFileRepositoryBase, ISettingsRepository
{
    public Task<T> GetAsync<T>(string key)
    {
        return DeserializeFileAsync<T>($"Data/Services/{key}.yaml");
    }

    public Task SaveAsync<T>(string key, T value)
    {
        return SerializeFileAsync($"Data/Services/{key}.yaml", value);
    }
}