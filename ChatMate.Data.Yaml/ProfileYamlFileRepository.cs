using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;

namespace ChatMate.Data.Yaml;

public class ProfileYamlFileRepository : YamlFileRepositoryBase, IProfileRepository
{
    public Task<ProfileSettings> GetProfileAsync()
    {
        return DeserializeFileAsync<ProfileSettings>("Data/Profile.yaml");
    }

    public Task SaveProfileAsync(ProfileSettings value)
    {
        return SerializeFileAsync("Data/Profile.yaml", value);
    }
}