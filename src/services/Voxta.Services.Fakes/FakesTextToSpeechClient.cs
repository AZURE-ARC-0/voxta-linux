using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Services;
using NAudio.Utils;
using NAudio.Wave;

namespace Voxta.Services.Fakes;

public class FakesTextToSpeechClient : ITextToSpeechService
{
    public string ServiceName => FakesConstants.ServiceName;
    public string ContentType => "audio/x-wav";
    
    public string[] Features => new[] { ServiceFeatures.NSFW };

    public Task<bool> InitializeAsync(string[] prerequisites, string culture, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
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
        var waveFormat = new WaveFormat(44100, 16, 1);
        var waveFile = new WaveFileWriter(new IgnoreDisposeStream(ms), waveFormat);
        waveFile.Close();
        ms.Seek(0, SeekOrigin.Begin);
        await tunnel.SendAsync(new AudioData(ms, ContentType), cancellationToken);
    }

    public void Dispose()
    {
    }
}