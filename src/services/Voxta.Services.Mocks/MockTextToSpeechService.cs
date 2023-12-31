﻿using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Services;
using NAudio.Utils;
using NAudio.Wave;
using Voxta.Abstractions.Repositories;

namespace Voxta.Services.Mocks;

public class MockTextToSpeechService : MockServiceBase, ITextToSpeechService
{
    public string ContentType => "audio/x-wav";

    public MockTextToSpeechService(ISettingsRepository settingsRepository) : base(settingsRepository)
    {
    }

    public string[] GetThinkingSpeech()
    {
        return Array.Empty<string>();
    }

    public Task<VoiceInfo[]> GetVoicesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new VoiceInfo[]
        {
            new()
            {
                Id = "fake",
                Label = "Fake",
            }
        });
    }

    public async Task GenerateSpeechAsync(SpeechRequest request, ISpeechTunnel tunnel, CancellationToken cancellationToken)
    {
        var ms = new MemoryStream();
        GenerateSineWave(440, 0.2, ms);
        ms.Seek(0, SeekOrigin.Begin);
        await tunnel.SendAsync(new AudioData(ms, ContentType), cancellationToken);
    }

    private static void GenerateSineWave(double frequency, double duration, Stream outputStream)
    {
        var sampleRate = 44100;
        var samples = (int)(sampleRate * duration);
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);

        using var writer = new WaveFileWriter(new IgnoreDisposeStream(outputStream), waveFormat);
        var angleStep = 2.0 * Math.PI * frequency / sampleRate;

        for (var i = 0; i < samples; i++)
        {
            var value = (float)Math.Sin(i * angleStep);
            writer.WriteSample(value);
        }
    }
}