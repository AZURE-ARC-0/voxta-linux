using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Abstractions.Services;
using NAudio.Utils;
using NAudio.Wave;

namespace ChatMate.Services.Fakes;

public class FakesTextToSpeechClient : ITextToSpeechService
{
    public string ServiceName => FakesConstants.ServiceName;
    public string ContentType => "audio/x-wav";

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
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
}