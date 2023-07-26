using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Repositories;

public interface IProfileRepository
{
    Task<ProfileSettings?> GetProfileAsync(CancellationToken cancellationToken);
    Task SaveProfileAsync(ProfileSettings profile);
}

public static class ProfileRepositoryExtensions
{
    public static async Task<ProfileSettings> GetRequiredProfileAsync(this IProfileRepository profileRepository, CancellationToken cancellationToken)
    {
        return await profileRepository.GetProfileAsync(cancellationToken) ?? throw new NullReferenceException("The profile was not configured.");
    }
}