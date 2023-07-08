using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;

namespace ChatMate.Abstractions.Services;

public interface ITextToSpeechService : IService
{
    string ServiceName { get; }
    string[]? GetThinkingSpeech();
    Task GenerateSpeechAsync(SpeechRequest request, ISpeechTunnel tunnel, string extension, CancellationToken cancellationToken);
}