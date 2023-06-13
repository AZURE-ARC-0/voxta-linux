using System.Buffers;
using System.Net.Sockets;
using System.Text;
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
    private const int _bufferSize = 1024;
    
    private static readonly byte[] EventPrefix = "event: "u8.ToArray();
    private static readonly byte[] DataPrefix = "data: "u8.ToArray();
    
    private readonly ILogger<ChatMateConnection> _logger;
    private readonly ITextGenService _textGen;
    private readonly ITextToSpeechService _speechGen;
    private readonly HttpProxyHandlerFactory _proxyHandlerFactory;
    private readonly ChatData _chatData;
    private readonly Guid _clientId;

    public ChatMateConnection(ILoggerFactory loggerFactory, ITextGenService textGen, ITextToSpeechService speechGen, HttpProxyHandlerFactory proxyHandlerFactory)
    {
        _logger = loggerFactory.CreateLogger<ChatMateConnection>();
        _textGen = textGen;
        _speechGen = speechGen;
        _proxyHandlerFactory = proxyHandlerFactory;
        _chatData = new ChatData();
        _clientId = Crypto.CreateCryptographicallySecureGuid();
    }

    public async Task ProcessClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        _logger.BeginScope("Client {ClientId}", _clientId);

        using (client)
        {
            try
            {
                _logger.LogInformation("Client connected");
                var stream = client.GetStream();

                var buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
                var lineBuffer = new MemoryStream();
                string? currentEvent = null;
                try
                {
                    while (!cancellationToken.IsCancellationRequested && client.Connected)
                    {
                        var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                        if (bytesRead == 0)
                            break;

                        var processedBytes = 0;

                        while (processedBytes < bytesRead)
                        {
                            var lineBreakPosition = Array.IndexOf(buffer, (byte)'\n', processedBytes, bytesRead - processedBytes);

                            if (lineBreakPosition >= 0) // If a line break is found
                            {
                                lineBuffer.Write(buffer, processedBytes, lineBreakPosition - processedBytes);

                                var line = lineBuffer.GetBuffer().AsMemory(0, (int)lineBuffer.Length);

                                if (line.Span.StartsWith(EventPrefix))
                                {
                                    currentEvent = Encoding.ASCII.GetString(line[EventPrefix.Length..].Span).Trim();
                                }
                                else if (line.Span.StartsWith(DataPrefix) && currentEvent != null)
                                {
                                    var eventData = line[DataPrefix.Length..];

                                    if (currentEvent == "send")
                                    {
                                        var clientMessage = JsonSerializer.Deserialize<Message>(eventData.Span);
                                        _logger.LogDebug("Received chat message: {Text}", clientMessage.Content);
                                        var gen = await _textGen.GenerateTextAsync(_chatData, clientMessage.Content);
                                        _logger.LogDebug("Generated chat reply: {Text}", gen);
                                        await SendJson(stream, new Message { Type = "Reply", Content = gen }, cancellationToken);
                                        cancellationToken.ThrowIfCancellationRequested();
                                        var speechUrl = await _speechGen.GenerateSpeechUrlAsync(gen);
                                        _logger.LogDebug("Generated speech URL: {SpeechUrl}", speechUrl);
                                        await SendJson(stream, new Message { Type = "Speech", Content = speechUrl }, cancellationToken);
                                    }

                                    currentEvent = null; // Reset the current event
                                }

                                lineBuffer.SetLength(0); // Clear the line buffer
                                processedBytes = lineBreakPosition + 1; // Move to next line
                            }
                            else
                            {
                                // If no line break, write all remaining bytes to lineBuffer
                                lineBuffer.Write(buffer, processedBytes, bytesRead - processedBytes);
                                processedBytes = bytesRead; // All bytes processed
                            }
                        }
                    }

                    if (lineBuffer.Length > 0)
                    {
                        // Handle the last line if it doesn't end with a line break
                        throw new InvalidOperationException("Unfinished line received: " +
                                                            Encoding.UTF8.GetString(lineBuffer.GetBuffer().AsMemory(0, (int)lineBuffer.Length).Span));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error");
                    await SendJson(stream, new Message { Type = "error", Content = $"Server exception: {ex.Message}" }, cancellationToken);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    await lineBuffer.DisposeAsync();
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