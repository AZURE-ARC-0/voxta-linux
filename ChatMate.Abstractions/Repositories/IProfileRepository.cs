using ChatMate.Abstractions.Model;

namespace ChatMate.Abstractions.Repositories;

public interface IProfileRepository
{
    Task<ProfileSettings> GetProfileAsync();
    Task SaveProfileAsync(ProfileSettings value);
}