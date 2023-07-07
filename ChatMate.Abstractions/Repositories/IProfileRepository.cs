using ChatMate.Abstractions.Model;

namespace ChatMate.Abstractions.Repositories;

public interface IProfileRepository
{
    Task<ProfileSettings?> GetProfileAsync(CancellationToken cancellationToken);
    Task SaveProfileAsync(ProfileSettings value);
}