namespace Voxta.Abstractions.Model;

public sealed class AudioData
{
    public string ContentType { get; }
    public Stream Stream { get; }
    
    public AudioData(Stream stream, string contentType)
    {
        Stream = stream;
        ContentType = contentType;
    }

    public static string GetExtension(string contentType)
    {
        return contentType switch
        {
            "audio/mpeg" => "mp3",
            "audio/x-wav" or "audio/wav" => "wav",
            "audio/webm" => "webm",
            _ => throw new NotSupportedException($"Content type '{contentType}' is not supported.")
        };
    }

    public static string FromExtension(string extension)
    {
        return extension switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/x-wav",
            ".webm" => "audio/webm",
            ".m4a" => "audio/aac",
            _ => throw new NotSupportedException($"Extension '{extension}' is not supported.")
        };
    }
}