using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ChatMate.Server;

public class ChatSessionFactory
{
    private readonly ITextGenService _textGen;
    private readonly PendingSpeechManager _pendingSpeech;
    private readonly IAnimationSelectionService _animSelect;
    private readonly ILogger<ChatSession> _logger;

    [SuppressMessage("ReSharper", "ContextualLoggerProblem")]
    public ChatSessionFactory(ITextGenService textGen, PendingSpeechManager pendingSpeech, IAnimationSelectionService animSelect, ILogger<ChatSession> logger)
    {
        _textGen = textGen;
        _pendingSpeech = pendingSpeech;
        _animSelect = animSelect;
        _logger = logger;
    }
    
    public ChatSession Create(WebSocket webSocket)
    {
        return new ChatSession(webSocket, _textGen, _pendingSpeech, _animSelect, _logger);
    }
}

public class ChatSession
{
    private readonly JsonSerializerOptions _serializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
    
    private readonly WebSocket _webSocket;
    private readonly ITextGenService _textGen;
    private readonly PendingSpeechManager _pendingSpeech;
    private readonly IAnimationSelectionService _animSelect;
    private readonly ILogger<ChatSession> _logger;
    private readonly SemaphoreSlim _sendLock = new(1);
    
    private ChatData? _chatData;

    public ChatSession(WebSocket webSocket, ITextGenService textGen, PendingSpeechManager pendingSpeech, IAnimationSelectionService animSelect, ILogger<ChatSession> logger)
    {
        _webSocket = webSocket;
        _textGen = textGen;
        _pendingSpeech = pendingSpeech;
        _animSelect = animSelect;
        _logger = logger;
    }

    private static string Replace(IReadOnlyChatData chatData, string text) => text
        .Replace("{{Now}}", DateTime.Now.ToString("f", CultureInfo.InvariantCulture))
        .Replace("{{Bot}}", chatData.BotName);
    
    public async Task HandleWebSocketConnectionAsync(CancellationToken cancellationToken)
    {   
        var buffer = new byte[1024 * 4];

        await SendAsync(new ServerBotsListMessage
        {
            Bots = new[]
            {
                new ServerBotsListMessage.Bot
                {
                    Id = "Melly",
                    Name = "Melly"
                }
            }
        }, cancellationToken);

        while (!_webSocket.CloseStatus.HasValue)
        {
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.CloseStatus.HasValue) return;

            try
            {
                var clientMessage = JsonSerializer.Deserialize<ClientMessage>(buffer.AsMemory(0, result.Count).Span, _serializeOptions);

                // TODO: Select a bot from the provided bots list and a conversation ID to load the  chat

                switch (clientMessage)
                {
                    case ClientSelectBotMessage selectBotMessage:
                        await HandleSelectBotMessage(selectBotMessage, cancellationToken);
                        break;
                    case ClientSendMessage sendMessage:
                        await HandleClientMessage(sendMessage, cancellationToken);
                        break;
                    default:
                        _logger.LogError("Unknown message type {ClientMessage}", clientMessage?.GetType().Name ?? "null");
                        break;
                }
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "Error processing socket message: {RawMessage}", Encoding.UTF8.GetString(buffer.AsMemory(0, result.Count).Span));
                await SendAsync(new ServerErrorMessage
                {
                    Message = exc.Message,
                }, cancellationToken);
            }
        }
    }

    private async Task HandleSelectBotMessage(ClientSelectBotMessage selectBotMessage, CancellationToken cancellationToken)
    {
        _chatData = null;
        
        if (string.IsNullOrEmpty(selectBotMessage.BotId))
        {
            return;
        }
        
        _logger.LogInformation("Received bot selection: {BotId}", selectBotMessage.BotId);

        if (selectBotMessage.BotId != "Melly")
        {
            await SendAsync(new ServerErrorMessage
            {
                Message = $"Unknown bot {selectBotMessage.BotId}"
            }, cancellationToken);
            return;
        }
        
        // TODO: Use a real chat data store, reload using auth
        var chatData = new ChatData
        {
            Id = Crypto.CreateCryptographicallySecureGuid()
        };
        chatData.Preamble.Text = Replace(chatData, chatData.Preamble.Text);
        chatData.Preamble.Tokens = _textGen.GetTokenCount(chatData.Preamble);
        foreach (var message in chatData.Messages)
        {
            message.Text = Replace(chatData, message.Text);
            message.User = Replace(chatData, message.User);
        }
        _chatData = chatData;
        await SendAsync(new ServerReadyMessage(), cancellationToken);
    }

    private async Task HandleClientMessage(ClientSendMessage sendMessage, CancellationToken cancellationToken)
    {
        if (_chatData is null) throw new InvalidOperationException("Chat data is null");
        
        _logger.LogInformation("Received chat message: {Text}", sendMessage.Text);
        // TODO: Save into some storage
        _chatData.Messages.Add(new ChatMessageData
        {
            Id = Guid.NewGuid(),
            User = _chatData.UserName,
            Timestamp = DateTimeOffset.UtcNow,
            Text = sendMessage.Text,
        });

        var gen = await _textGen.GenerateReplyAsync(_chatData);
        var reply = new ChatMessageData
        {
            Id = Guid.NewGuid(),
            User = _chatData.BotName,
            Timestamp = DateTimeOffset.UtcNow,
            Text = gen.Text,
            Tokens = gen.Tokens,
        };
        _logger.LogInformation("Reply ({Tokens} tokens): {Text}", reply.Tokens, reply.Text);
        // TODO: Save into some storage
        _chatData.Messages.Add(reply);
        _pendingSpeech.Push(_chatData.Id, reply.Id, new SpeechRequest
        {
            Text = gen.Text
        });
        await SendAsync(new ServerReplyMessage
        {
            Text = reply.Text,
            SpeechUrl = $"/chats/{_chatData.Id}/messages/{reply.Id}/speech/{_chatData.Id}_{reply.Id}.wav"
            
        }, cancellationToken);

        var animation = await _animSelect.SelectAnimationAsync(_chatData);
        _logger.LogInformation("Selected animation: {Animation}", animation);
        await SendAsync(new ServerAnimationMessage { Value = animation }, cancellationToken);
    }

    private async Task SendAsync<T>(T message, CancellationToken cancellationToken) where T : ServerMessage
    {
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await _webSocket.SendAsync(
                JsonSerializer.SerializeToUtf8Bytes<ServerMessage>(message, _serializeOptions),
                WebSocketMessageType.Text,
                true,
                cancellationToken
            );
        }
        finally
        {
            _sendLock.Release();
        }
    }
}
