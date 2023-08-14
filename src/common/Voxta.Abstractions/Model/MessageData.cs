namespace Voxta.Abstractions.Model;

[Serializable]
public class MessageData : TextData
{
    public required ChatMessageRole Role { get; init; }
}