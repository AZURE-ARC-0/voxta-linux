using Voxta.Abstractions.Model;

namespace Voxta.Host.AspNetCore.WebSockets.Utils;

public static class ProfileUtils
{
    public static ProfileSettings CreateDefaultProfile()
    {
        return new ProfileSettings
        {
            Name = "User",
        };
    }
}