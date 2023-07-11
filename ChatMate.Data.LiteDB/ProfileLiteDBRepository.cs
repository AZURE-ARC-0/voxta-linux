
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;
using LiteDB;

namespace ChatMate.Data.LiteDB;

public class ProfileLiteDBRepository : IProfileRepository
{
    private readonly ILiteCollection<ProfileSettings> _profilesCollection;

    public ProfileLiteDBRepository(ILiteDatabase db)
    {
        _profilesCollection = db.GetCollection<ProfileSettings>();
    }
    
    public Task<ProfileSettings?> GetProfileAsync(CancellationToken cancellationToken)
    {
        var profile = _profilesCollection.FindOne(_ => true);
        return Task.FromResult<ProfileSettings?>(profile);
    }

    public Task SaveProfileAsync(ProfileSettings profile)
    {
        _profilesCollection.Upsert(profile);
        return Task.CompletedTask;
    }
}