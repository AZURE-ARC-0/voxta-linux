using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Repositories;

public interface IProfileRepository
{
    Task<ProfileSettings?> GetProfileAsync(CancellationToken cancellationToken);
    Task SaveProfileAsync(ProfileSettings profile);
}