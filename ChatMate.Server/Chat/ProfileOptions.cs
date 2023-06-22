namespace ChatMate.Server;

[Serializable]
public class ProfileOptions
{
    public string Name { get; init; } = "User";
    public string Description { get; init; } = "No description available";
}