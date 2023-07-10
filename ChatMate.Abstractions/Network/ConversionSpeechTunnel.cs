using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Services;

namespace ChatMate.Abstractions.Network;

public class ConversionSpeechTunnel : ISpeechTunnel
{
    private readonly ISpeechTunnel _tunnel;
    private readonly IAudioConverter _audioConverter;

    public ConversionSpeechTunnel(ISpeechTunnel tunnel, IAudioConverter audioConverter)
    {
        _tunnel = tunnel;
        _audioConverter = audioConverter;
    }

    public Task ErrorAsync(string message, CancellationToken cancellationToken)
    {
        return _tunnel.ErrorAsync(message, cancellationToken);
    }

    public async Task SendAsync(AudioData audioData, CancellationToken cancellationToken)
    {
        var converted = await _audioConverter.ConvertAudioAsync(audioData, cancellationToken);
        await _tunnel.SendAsync(converted, cancellationToken);
    }
}