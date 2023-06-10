using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

public class Program
{
    private static readonly ILogger Logger = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        })
        .CreateLogger<Program>();

    public static async Task Main(string[] args) => await Start(args, CancellationToken.None);

    public static async Task Start(string[] args, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ongoingTasks = new ConcurrentDictionary<Task, byte>();

        var listener = new TcpListener(IPAddress.Loopback, 5384);
        listener.Start();
        Logger.LogInformation("Server started: 0.0.0.0:5384");

        Console.CancelKeyPress += (sender, args) =>
        {
            Logger.LogInformation("Shutting down...");
            listener.Stop();
            cts.Cancel();
            args.Cancel = true;
        };

        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(cts.Token);
                var clientProcessingTask = ProcessClient(client, Guid.NewGuid(), cts.Token);
                ongoingTasks.TryAdd(clientProcessingTask, 0);
                _ = clientProcessingTask.ContinueWith(t => ongoingTasks.TryRemove(t, out _));
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        await Task.WhenAll(ongoingTasks.Keys);
    }


    private static async Task ProcessClient(TcpClient client, Guid clientId, CancellationToken cancellationToken)
    {
        using (client)
        {
            try
            {
                Logger.LogInformation($"Client {clientId} connected...");
                var stream = client.GetStream();

                await SendJson(stream, new Message { Type = "success", Content = "Connection established" }, cancellationToken);

                byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
                try
                {
                    while (!cancellationToken.IsCancellationRequested && client.Connected && client.Available != 0)
                    {
                        int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                        if (bytesRead == 0)
                            break;

                        var clientMessage = JsonSerializer.Deserialize<Message>(buffer.AsMemory(0, bytesRead).Span);

                        if (clientMessage == null) continue;

                        if (clientMessage.Type == "disconnect")
                        {
                            Logger.LogInformation($"Client {clientId} requested to disconnect");
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
                    Logger.LogError(ex, $"Error while processing client {clientId}.");
                    await SendJson(stream, new Message { Type = "error", Content = "Something went wrong" }, cancellationToken);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation($"Client {clientId} processing cancelled.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error while processing client {clientId}.");
            }
        }
    }

    private static async Task SendJson(NetworkStream stream, Message message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(message);
        await stream.WriteAsync(json.AsMemory(), cancellationToken);
    }
}

public class Message
{
    public required string Type { get; set; }
    public required string Content { get; set; }
}
