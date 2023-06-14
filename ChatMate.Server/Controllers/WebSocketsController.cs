using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server;

[ApiController]
public class WebSocketsController : ControllerBase
{
    private readonly ILogger<WebSocketsController> _logger;
    private readonly ITextGenService _textGen;
    private readonly ITextToSpeechService _speechGen;

    public WebSocketsController(ILogger<WebSocketsController> logger, ITextGenService textGen, ITextToSpeechService speechGen)
    {
        _logger = logger;
        _textGen = textGen;
        _speechGen = speechGen;
    }
    
    [HttpGet("/ws")]
    public async Task WebSocket(CancellationToken cancellationToken)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }
        
        var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        _logger.Log(LogLevel.Information, "WebSocket connection established");
        try
        {
            await HandleWebSocketConnection(webSocket, cancellationToken);
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Error in websocket connection");
            if (webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived or WebSocketState.CloseSent)
                await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, exc.Message, CancellationToken.None);
        }
        finally
        {
            _logger.Log(LogLevel.Information, "WebSocket connection closed");
        }
    }
    
    private async Task HandleWebSocketConnection(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 4];
        
        // TODO: Use a real chat data store
        var chatData = new ChatData();

        while(!webSocket.CloseStatus.HasValue)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.CloseStatus.HasValue) return;
            
            var clientMessage = JsonSerializer.Deserialize<Message>(buffer.AsMemory(0, result.Count).Span);
            if (clientMessage == null)
            {
                continue;
            }
            if (clientMessage.Type == "Send")
            {
                _logger.LogDebug("Received chat message: {Text}", clientMessage.Content);
                var gen = await _textGen.GenerateTextAsync(chatData, clientMessage.Content);
                _logger.LogDebug("Generated chat reply: {Text}", gen);
                await webSocket.SendAsync(
                    JsonSerializer.SerializeToUtf8Bytes(new Message { Type = "Reply", Content = gen }),
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken
                    );
                    
                var speechUrl = await _speechGen.GenerateSpeechUrlAsync(gen);
                _logger.LogDebug("Generated speech URL: {SpeechUrl}", speechUrl);
                await webSocket.SendAsync(
                    JsonSerializer.SerializeToUtf8Bytes(new Message { Type = "Speech", Content = speechUrl }),
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken
                );
            } 
        }
    }
}