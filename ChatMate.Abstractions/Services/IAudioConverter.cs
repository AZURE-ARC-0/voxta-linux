using ChatMate.Abstractions.Model;

namespace ChatMate.Abstractions.Services;

public interface IAudioConverter
{
    string ContentType { get; }
    Task<AudioData> ConvertAudioAsync(AudioData input, CancellationToken cancellationToken);
    void SelectOutputContentType(string[] acceptedContentTypes, string sourceContentType);
}