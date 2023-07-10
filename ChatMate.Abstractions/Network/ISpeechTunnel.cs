using ChatMate.Abstractions.Model;

namespace ChatMate.Abstractions.Network;

public interface ISpeechTunnel
{
    Task ErrorAsync(string message, CancellationToken cancellationToken);
    Task SendAsync(AudioData audioData, CancellationToken cancellationToken);
}