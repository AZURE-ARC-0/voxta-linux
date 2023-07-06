namespace ChatMate.Abstractions.Network;

public interface ISpeechTunnel
{
    Task ErrorAsync(string message, CancellationToken cancellationToken);
    Task SendAsync(byte[] bytes, string contentType, CancellationToken cancellationToken);
}