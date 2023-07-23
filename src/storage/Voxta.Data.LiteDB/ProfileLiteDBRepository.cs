
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using LiteDB;

namespace Voxta.Data.LiteDB;

public class ProfileLiteDBRepository : IProfileRepository
{
    private readonly ILiteCollection<ProfileSettings> _profilesCollection;

    public ProfileLiteDBRepository(ILiteDatabase db)
    {
        _profilesCollection = db.GetCollection<ProfileSettings>();
    }

    public Task<ProfileSettings?> GetProfileAsync(CancellationToken cancellationToken)
    {
        var profile = _profilesCollection.FindOne(x => x.Id == ProfileSettings.SharedId);
        return Task.FromResult<ProfileSettings?>(profile);
    }

    public Task SaveProfileAsync(ProfileSettings profile)
    {
        _profilesCollection.Upsert(profile);
        return Task.CompletedTask;
    }
}