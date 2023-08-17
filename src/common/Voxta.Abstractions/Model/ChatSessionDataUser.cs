namespace Voxta.Abstractions.Model;

[Serializable]
public class ChatSessionDataUser
{
    public required TextData Name { get; init; }
    public TextData Description { get; init; } = TextData.Empty;
}