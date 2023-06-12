using System.Buffers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ChatMate.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatMate.Server;

public class ChatMateConnectionFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ChatMateConnectionFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ChatMateConnection Create()
    {
        return _serviceProvider.GetRequiredService<ChatMateConnection>();
    }
}

public class ChatMateConnection
{
    private readonly ILogger<ChatMateConnection> _logger;
    private readonly ITextGenService _textGen;
    private readonly ITextToSpeechService _speechGen;

    public ChatMateConnection(ILoggerFactory loggerFactory, ITextGenService textGen, ITextToSpeechService speechGen)
    {
        _logger = loggerFactory.CreateLogger<ChatMateConnection>();
        _textGen = textGen;
        _speechGen = speechGen;
    }

    public async Task ProcessClientAsync(TcpClient client, Guid clientId, CancellationToken cancellationToken)
    {
        using (client)
        {
            try
            {
                _logger.LogInformation("Client {ClientId} connected...", clientId);
                var stream = client.GetStream();

                var buffer = ArrayPool<byte>.Shared.Rent(1024);
                try
                {
                    while (!cancellationToken.IsCancellationRequested && client.Connected)
                    {
                        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                        if (bytesRead == 0)
                            break;

                        var memory = buffer.AsMemory(0, bytesRead);

                        if (memory.Span.StartsWith("GET "u8))
                        {
                            try
                            {
                                var rawRequest = Encoding.UTF8.GetString(memory.Span);
                                // TODO: Some kind of router
                                await _speechGen.HandleSpeechRequest(rawRequest, stream);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error while processing HTTP GET {ClientId}", clientId);
                            }
                        }
                        else if (memory.Span.StartsWith("{"u8))
                        {
                            try
                            {
                                var clientMessage = JsonSerializer.Deserialize<Message>(memory.Span);

                                if (clientMessage == null) continue;

                                if (clientMessage.Type == "disconnect")
                                {
                                    _logger.LogInformation("Client {ClientId} requested to disconnect", clientId);
                                    break;
                                }

                                if (clientMessage.Type == "chat")
                                {
                                    var gen = await _textGen.GenerateTextAsync(clientMessage.Content);
                                    await SendJson(stream, new Message { Type = "message", Content = gen }, cancellationToken);
                                    cancellationToken.ThrowIfCancellationRequested();
                                    var speechUrl = await _speechGen.GenerateSpeechUrlAsync(gen);
                                    await SendJson(stream, new Message { Type = "speech", Content = speechUrl }, cancellationToken);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error while processing client {ClientId}", clientId);
                                await SendJson(stream, new Message { Type = "error", Content = $"Server exception: {ex.Message}" }, cancellationToken);
                            }
                        }
                        else
                        {
                            _logger.LogError("Unexpected packet for client {ClientId}", clientId);
                        }
                    }
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
            catch (IOException)
            {
                _logger.LogInformation("Client {ClientId} interrupted", clientId);
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
}