namespace ChatMate.Abstractions.Model;

public class SpeechRequest
{
    public required string Service { get; init; }
    public required string Text { get; init; }
    public required string Voice { get; init; }
}