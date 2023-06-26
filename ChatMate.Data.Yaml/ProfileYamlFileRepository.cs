using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;

namespace ChatMate.Data.Yaml;

public class ProfileYamlFileRepository : YamlFileRepositoryBase, IProfileRepository
{
    private ProfileSettings? _cache;
    
    public async Task<ProfileSettings?> GetProfileAsync()
    {
        if (_cache != null) return _cache;
        return _cache = await DeserializeFileAsync<ProfileSettings>("Data/Profile.yaml");
    }

    public Task SaveProfileAsync(ProfileSettings value)
    {
        _cache = value;
        return SerializeFileAsync("Data/Profile.yaml", value);
    }
}