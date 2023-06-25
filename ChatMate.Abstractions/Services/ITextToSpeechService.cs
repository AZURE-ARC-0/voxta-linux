using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;

namespace ChatMate.Abstractions.Services;

public interface ITextToSpeechService
{
    Task GenerateSpeechAsync(SpeechRequest request, ISpeechTunnel tunnel, string extension);
}