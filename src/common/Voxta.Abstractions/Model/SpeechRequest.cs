namespace Voxta.Abstractions.Model;

public class SpeechRequest
{
    public required string ServiceName { get; init; }
    public required Guid? ServiceId { get; init; }
    public required string Text { get; init; }
    public required string Voice { get; init; }
    public required string ContentType { get; init; }
    public required string Culture { get; init; }
    public bool Reusable { get; init; }
}