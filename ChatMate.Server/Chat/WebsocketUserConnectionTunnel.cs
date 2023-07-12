using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;

namespace ChatMate.Server.Chat;

public class WebsocketUserConnectionTunnel : IUserConnectionTunnel
{
    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new BooleanStringJsonConverter(), new NullableGuidJsonConverter() }
    };

    private class BooleanStringJsonConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.String:
                    var stringValue = reader.GetString();
                    return bool.TryParse(stringValue, out var value) && value;
                default:
                    throw new JsonException("Invalid boolean value");
            }
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    private class NullableGuidJsonConverter : JsonConverter<Guid?>
    {
        public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrEmpty(stringValue)) return null;
            return Guid.Parse(stringValue);
        }

        public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
    
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
            return await JsonSerializer.DeserializeAsync<T>(message, SerializeOptions, cancellationToken);
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