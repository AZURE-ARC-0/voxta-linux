using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;

namespace Voxta.Abstractions.Services;

public interface ITextToSpeechService : IService
{
    string ServiceName { get; }
    string ContentType { get; }
    string[]? GetThinkingSpeech();
    Task<VoiceInfo[]> GetVoicesAsync(CancellationToken cancellationToken);
    Task GenerateSpeechAsync(SpeechRequest request, ISpeechTunnel tunnel, CancellationToken cancellationToken);
}