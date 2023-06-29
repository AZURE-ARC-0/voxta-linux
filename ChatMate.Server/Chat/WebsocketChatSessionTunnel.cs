using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;

namespace ChatMate.Server.Chat;

public class WebsocketChatSessionTunnel : IChatSessionTunnel
{
    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
    
    private readonly byte[] _buffer = new byte[1024 * 4];
    
    private readonly WebSocket _webSocket;
    private readonly SemaphoreSlim _sendLock = new(1);

    public bool Closed => _webSocket.CloseStatus.HasValue;

    public WebsocketChatSessionTunnel(WebSocket webSocket)
    {
        _webSocket = webSocket;
    }

    public async Task<T?> ReceiveAsync<T>(CancellationToken cancellationToken) where T : ClientMessage
    {
        var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(_buffer), cancellationToken);
        if (result.CloseStatus.HasValue) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(_buffer.AsMemory(0, result.Count).Span, SerializeOptions);
        }
        catch (JsonException exc)
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
                JsonSerializer.SerializeToUtf8Bytes<ServerMessage>(message, SerializeOptions),
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