using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Network;

public interface ISpeechTunnel
{
    Task ErrorAsync(Exception exc, CancellationToken cancellationToken);
    Task SendAsync(AudioData audioData, CancellationToken cancellationToken);
}