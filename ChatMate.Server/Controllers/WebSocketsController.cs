using System.Net.Sockets;
using System.Net.WebSockets;
using ChatMate.Core;
using ChatMate.Server.Chat;
using Microsoft.AspNetCore.Mvc;

namespace ChatMate.Server.Controllers;

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
        var tunnel = new WebsocketUserConnectionTunnel(webSocket);
        using var chatSession = _chatInstanceFactory.Create(tunnel);
        _logger.LogInformation("WebSocket connection established");
        try
        {
            await chatSession.HandleWebSocketConnectionAsync(cancellationToken);
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            _logger.LogInformation("WebSocket connection closed");
        }
        catch (SocketException exc)
        {
            _logger.LogWarning(exc, "Unexpected socket exception");
        }
        catch (WebSocketException exc)
        {
            if (webSocket.State == WebSocketState.Aborted)
            {
                _logger.LogInformation("WebSocket connection aborted");
                return;
            }

            _logger.LogWarning(exc, "Unexpected websocket exception");
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Unexpected chat session error: {Message}", exc.Message);
        }
    }
}