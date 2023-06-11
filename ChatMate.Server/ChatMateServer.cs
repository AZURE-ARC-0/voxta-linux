using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ChatMate.Server;

public sealed class ChatMateServer : IDisposable
{
    private readonly ILogger<ChatMateServer> _logger;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public ChatMateServer(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ChatMateServer>();
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        _listener = new TcpListener(IPAddress.Loopback, 5384);
        _listener.Start();
        _logger.LogInformation("Server started: 0.0.0.0:5384");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var ongoingTasks = new ConcurrentDictionary<Task, byte>();
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                var clientProcessingTask = ProcessClient(client, Guid.NewGuid(), _cts.Token);
                ongoingTasks.TryAdd(clientProcessingTask, 0);
                _ = clientProcessingTask.ContinueWith(t => ongoingTasks.TryRemove(t, out _), _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        await Task.WhenAll(ongoingTasks.Keys);
    }

    public void Stop()
    {
        _logger.LogInformation("Shutting down...");
        _listener?.Stop();
        _cts?.Cancel();
    }

    private async Task ProcessClient(TcpClient client, Guid clientId, CancellationToken cancellationToken)
    {
        using (client)
        {
            try
            {
                _logger.LogInformation("Client {ClientId} connected...", clientId);
                var stream = client.GetStream();

                await SendJson(stream, new Message { Type = "success", Content = "Connection established" }, cancellationToken);

                var buffer = ArrayPool<byte>.Shared.Rent(1024);
                try
                {
                    while (!cancellationToken.IsCancellationRequested && client.Connected)
                    {
                        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                        if (bytesRead == 0)
                            break;

                        var clientMessage = JsonSerializer.Deserialize<Message>(buffer.AsMemory(0, bytesRead).Span);

                        if (clientMessage == null) continue;

                        if (clientMessage.Type == "disconnect")
                        {
                            _logger.LogInformation("Client {ClientId} requested to disconnect", clientId);
                            break;
                        }

                        if (clientMessage.Type == "chat")
                        {
                            await SendJson(stream, new Message { Type = "waiting", Content = "Waiting" }, cancellationToken);
                            cancellationToken.ThrowIfCancellationRequested();
                            // Simulate processing
                            await Task.Delay(2000, cancellationToken);
                            await SendJson(stream, new Message { Type = "message", Content = "Message" }, cancellationToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing client {ClientId}", clientId);
                    await SendJson(stream, new Message { Type = "error", Content = "Something went wrong" }, cancellationToken);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Client {ClientId} processing cancelled", clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing client {ClientId}", clientId);
            }
        }
    }

    private static async Task SendJson(NetworkStream stream, Message message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(message);
        await stream.WriteAsync(json.AsMemory(), cancellationToken);
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}