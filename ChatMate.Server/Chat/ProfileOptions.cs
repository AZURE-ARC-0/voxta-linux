using System.ComponentModel.DataAnnotations;

namespace ChatMate.Server;

[Serializable]
public class ProfileOptions
{
    [MinLength(1)]
    public required string Name { get; init; }
    public string Description { get; init; } = "No description available";
}