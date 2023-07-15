using ChatMate.Abstractions.Model;

namespace ChatMate.Abstractions.Network;

public interface IUserConnectionTunnel
{
    bool Closed { get; }
    Task<T?> ReceiveAsync<T>(CancellationToken cancellationToken) where T : ClientMessage;
    Task SendAsync<T>(T message, CancellationToken cancellationToken) where T : ServerMessage;
}