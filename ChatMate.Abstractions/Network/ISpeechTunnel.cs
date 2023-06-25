namespace ChatMate.Abstractions.Network;

public interface ISpeechTunnel
{
    Task ErrorAsync(string message);
    Task SendAsync(byte[] bytes, string contentType);
}