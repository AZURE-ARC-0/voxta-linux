using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server;

[ApiController]
public class WebSocketsController : ControllerBase
{
    private readonly ILogger<WebSocketsController> _logger;
    private readonly ChatSessionFactory _chatInstanceFactory;

    public WebSocketsController(ILogger<WebSocketsController> logger, ChatSessionFactory chatInstanceFactory)
    {
        _logger = logger;
        _chatInstanceFactory = chatInstanceFactory;
    }
    
    [HttpGet("/ws")]
    public async Task WebSocket(CancellationToken cancellationToken)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }
        
        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var chatSession = _chatInstanceFactory.Create(webSocket);
        _logger.Log(LogLevel.Information, "WebSocket connection established");
        try
        {
            await chatSession.HandleWebSocketConnectionAsync(cancellationToken);
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Error in websocket connection");
        }
        finally
        {
            _logger.Log(LogLevel.Information, "WebSocket connection closed");
        }
    }
}