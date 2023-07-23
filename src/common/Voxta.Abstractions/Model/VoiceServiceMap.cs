namespace Voxta.Abstractions.Model;

[Serializable]
public class VoiceServiceMap : ServiceMap
{
    public required string Voice { get; init; }
}