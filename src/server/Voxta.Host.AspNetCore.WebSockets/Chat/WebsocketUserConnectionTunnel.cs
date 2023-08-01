using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Common;

namespace Voxta.Host.AspNetCore.WebSockets;

public class WebsocketUserConnectionTunnel : IUserConnectionTunnel
{
    private readonly byte[] _buffer = new byte[1024 * 4];
    
    private readonly WebSocket _webSocket;
    private readonly SemaphoreSlim _sendLock = new(1);

    public bool Closed => _webSocket.CloseStatus.HasValue;

    public WebsocketUserConnectionTunnel(WebSocket webSocket)
    {
        _webSocket = webSocket;
    }

    public async Task<T?> ReceiveAsync<T>(CancellationToken cancellationToken) where T : ClientMessage
    {
        WebSocketReceiveResult result;
        using var message = new MemoryStream();
        do
        {
            result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(_buffer), cancellationToken);
            if (result.CloseStatus.HasValue) return null;
            message.Write(_buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);
        
        try
        {
            message.Seek(0, SeekOrigin.Begin);
            return await JsonSerializer.DeserializeAsync<T>(message, VoxtaJsonSerializer.SerializeOptions, cancellationToken);
        }
        catch (Exception exc)
        {
            throw new JsonException($"Failed to deserialize: {Encoding.UTF8.GetString(_buffer.AsMemory(0, result.Count).Span)}", exc);
        }
    }

    public async Task SendAsync<T>(T message, CancellationToken cancellationToken) where T : ServerMessage
    {
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await _webSocket.SendAsync(
                JsonSerializer.SerializeToUtf8Bytes<ServerMessage>(message, VoxtaJsonSerializer.SerializeOptions),
                WebSocketMessageType.Text,
                true,
                cancellationToken
            );
        }
        finally
        {
            _sendLock.Release();
        }
    }
}