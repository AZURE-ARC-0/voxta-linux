using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace ChatMate.Server;

public sealed class ChatMateServer : IDisposable
{
    private readonly ILogger<ChatMateServer> _logger;
    private readonly ChatMateConnectionFactory _connectionFactory;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public ChatMateServer(ILoggerFactory loggerFactory, ChatMateConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
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
                var clientProcessingTask = _connectionFactory
                    .Create()
                    .ProcessClientAsync(client, _cts.Token)
                    .ContinueWith(t => ongoingTasks.TryRemove(t, out _), _cts.Token);
                if (!clientProcessingTask.IsFaulted)
                    ongoingTasks.TryAdd(clientProcessingTask, 0);
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

    public void Dispose()
    {
        _cts?.Dispose();
    }
}