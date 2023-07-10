using ChatMate.Abstractions.Model;

namespace ChatMate.Abstractions.Services;

public interface IAudioConverter
{
    string ContentType { get; }
    Task<AudioData> ConvertAudioAsync(AudioData input, CancellationToken cancellationToken);
    void SelectContentType(string[] acceptedContentTypes, string generatedContentType);
}