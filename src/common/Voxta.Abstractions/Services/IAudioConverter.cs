using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Services;

public interface IAudioConverter
{
    string ContentType { get; }
    Task<AudioData> ConvertAudioAsync(AudioData input, CancellationToken cancellationToken);
    void SelectOutputContentType(string[] acceptedContentTypes, string sourceContentType);
}