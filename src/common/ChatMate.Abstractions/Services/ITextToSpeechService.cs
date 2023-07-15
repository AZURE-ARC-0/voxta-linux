using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;

namespace ChatMate.Abstractions.Services;

public interface ITextToSpeechService : IService
{
    string ServiceName { get; }
    string ContentType { get; }
    string[]? GetThinkingSpeech();
    Task<VoiceInfo[]> GetVoicesAsync(CancellationToken cancellationToken);
    Task GenerateSpeechAsync(SpeechRequest request, ISpeechTunnel tunnel, CancellationToken cancellationToken);
}