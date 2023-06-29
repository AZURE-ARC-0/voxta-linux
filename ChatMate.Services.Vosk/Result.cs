namespace ChatMate.Services.Vosk;

[Serializable]
public class PartialResult
{
    public required string Partial { get; init; }
}

[Serializable]
public class Result
{
    public required string Text { get; init; }
}