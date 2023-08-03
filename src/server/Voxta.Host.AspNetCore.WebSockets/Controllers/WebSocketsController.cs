using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Net.WebSockets;
using Voxta.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Voxta.Host.AspNetCore.WebSockets;

[ApiController]
public class WebSocketsController : ControllerBase
{
    private readonly ILogger<WebSocketsController> _logger;
    private readonly UserConnectionFactory _chatInstanceFactory;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public WebSocketsController(ILogger<WebSocketsController> logger, UserConnectionFactory chatInstanceFactory, IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _chatInstanceFactory = chatInstanceFactory;
        _applicationLifetime = applicationLifetime;
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
        await using var registration = _applicationLifetime.ApplicationStopping.Register([SuppressMessage("ReSharper", "AccessToDisposedClosure")] () =>
        {
            try
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutdown", CancellationToken.None).Wait(cancellationToken);
                }
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }
        });
        var tunnel = new WebsocketUserConnectionTunnel(webSocket);
        await using var userConnection = _chatInstanceFactory.Create(tunnel);
        _logger.LogInformation("WebSocket connection established");
        try
        {
            await userConnection.HandleWebSocketConnectionAsync(cancellationToken);
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            _logger.LogInformation("WebSocket connection closed");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Socket closed by cancellation");
        }
        catch (WebSocketException)
        {
            _logger.LogInformation("WebSocket connection closed: {State}", webSocket.State);
        }
        catch (SocketException exc)
        {
            _logger.LogWarning(exc, "Unexpected socket exception");
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Unexpected chat session error: {Message}", exc.Message);
        }
        finally
        {
            registration.Unregister();
        }
    }
}