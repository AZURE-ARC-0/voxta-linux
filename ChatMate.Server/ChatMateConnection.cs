using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
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
    private readonly HttpProxyHandlerFactory _proxyHandlerFactory;
    private readonly ChatData _chatData;

    public ChatMateConnection(ILoggerFactory loggerFactory, ITextGenService textGen, ITextToSpeechService speechGen, HttpProxyHandlerFactory proxyHandlerFactory)
    {
        _logger = loggerFactory.CreateLogger<ChatMateConnection>();
        _textGen = textGen;
        _speechGen = speechGen;
        _proxyHandlerFactory = proxyHandlerFactory;
        _chatData = new ChatData();
    }

    public async Task ProcessClientAsync(TcpClient client, Guid clientId, CancellationToken cancellationToken)
    {
        // TODO: Include in logs
        _logger.BeginScope("Client {ClientId}", clientId);
        
        using (client)
        {
            try
            {
                _logger.LogInformation("Client connected");
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
                            var proxy = _proxyHandlerFactory.Create(memory.Span, stream);
                            _logger.LogInformation("HTTP Request {ProxyMethod} {ProxyPath}", proxy.Method, proxy.Path);
                            try
                            {
                                if (proxy is { Method: "GET", Path: "/speech.mp3" })
                                {
                                    await _speechGen.HandleSpeechProxyRequestAsync(proxy);
                                }
                                else
                                {
                                    await proxy.WriteTextResponseAsync(HttpStatusCode.NotFound, "Not Found");
                                }
                            }
                            catch (Exception ex)
                            {
                                await proxy.WriteTextResponseAsync(HttpStatusCode.InternalServerError, ex.Message);
                                _logger.LogError(ex, "Error while processing HTTP GET {ClientId}", clientId);
                            }
                        }
                        else if (memory.Span.StartsWith("{"u8))
                        {
                            try
                            {
                                var clientMessage = JsonSerializer.Deserialize<Message>(memory.Span);

                                if (clientMessage == null)
                                {
                                    _logger.LogError("Could not deserialize message");
                                    continue;
                                }

                                _logger.LogInformation("Socket Request {Type} {Content}", clientMessage.Type, clientMessage.Content);

                                if (clientMessage.Type == "disconnect")
                                {
                                    _logger.LogInformation("Disconnect");
                                    break;
                                }

                                if (clientMessage.Type == "chat")
                                {
                                    var gen = await _textGen.GenerateTextAsync(_chatData, clientMessage.Content);
                                    await SendJson(stream, new Message { Type = "message", Content = gen }, cancellationToken);
                                    cancellationToken.ThrowIfCancellationRequested();
                                    var speechUrl = await _speechGen.GenerateSpeechUrlAsync(gen);
                                    await SendJson(stream, new Message { Type = "speech", Content = speechUrl }, cancellationToken);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error");
                                await SendJson(stream, new Message { Type = "error", Content = $"Server exception: {ex.Message}" }, cancellationToken);
                            }
                        }
                        else
                        {
                            _logger.LogError("Unexpected packet");
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
                _logger.LogInformation("Processing cancelled");
            }
            catch (IOException)
            {
                _logger.LogInformation("Interrupted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
            }
        }
    }

    private static async Task SendJson(NetworkStream stream, Message message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(message);
        await stream.WriteAsync(json.AsMemory(), cancellationToken);
    }
}