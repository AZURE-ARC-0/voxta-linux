﻿using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;

namespace Voxta.Abstractions.Network;

public class ConversionSpeechTunnel : ISpeechTunnel
{
    private readonly ISpeechTunnel _tunnel;
    private readonly IAudioConverter _audioConverter;

    public ConversionSpeechTunnel(ISpeechTunnel tunnel, IAudioConverter audioConverter)
    {
        _tunnel = tunnel;
        _audioConverter = audioConverter;
    }

    public Task ErrorAsync(Exception exc, CancellationToken cancellationToken)
    {
        return _tunnel.ErrorAsync(exc, cancellationToken);
    }

    public async Task SendAsync(AudioData audioData, CancellationToken cancellationToken)
    {
        var converted = await _audioConverter.ConvertAudioAsync(audioData, cancellationToken);
        await _tunnel.SendAsync(converted, cancellationToken);
    }
}