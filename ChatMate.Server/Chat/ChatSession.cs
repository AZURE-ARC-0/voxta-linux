using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ChatMate.Server;

public class ChatSessionFactory
{
    private readonly SelectorFactory<ITextGenService> _textGenFactory;
    private readonly PendingSpeechManager _pendingSpeech;
    private readonly SelectorFactory<IAnimationSelectionService> _animSelectFactory;
    private readonly ILogger<ChatSession> _logger;
    private readonly IBotRepository _bots;
    private readonly IOptions<ProfileOptions> _profile;

    [SuppressMessage("ReSharper", "ContextualLoggerProblem")]
    public ChatSessionFactory(SelectorFactory<ITextGenService> textGenFactory, PendingSpeechManager pendingSpeech, SelectorFactory<IAnimationSelectionService> animSelectFactory, ILogger<ChatSession> logger, IBotRepository bots, IOptions<ProfileOptions> profile)
    {
        _textGenFactory = textGenFactory;
        _pendingSpeech = pendingSpeech;
        _animSelectFactory = animSelectFactory;
        _logger = logger;
        _bots = bots;
        _profile = profile;
    }
    
    public ChatSession Create(WebSocket webSocket)
    {
        return new ChatSession(webSocket, _textGenFactory, _pendingSpeech, _animSelectFactory, _logger, _bots, _profile);
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
    private readonly SelectorFactory<ITextGenService> _textGenFactory;
    private readonly PendingSpeechManager _pendingSpeech;
    private readonly SelectorFactory<IAnimationSelectionService> _animSelectFactory;
    private readonly ILogger<ChatSession> _logger;
    private readonly IBotRepository _bots;
    private readonly IOptions<ProfileOptions> _profile;
    private readonly SemaphoreSlim _sendLock = new(1);

    private BotDefinition? _bot;
    private ChatData? _chatData;

    public ChatSession(WebSocket webSocket, SelectorFactory<ITextGenService> textGenFactory, PendingSpeechManager pendingSpeech, SelectorFactory<IAnimationSelectionService> animSelectFactory, ILogger<ChatSession> logger, IBotRepository bots, IOptions<ProfileOptions> profile)
    {
        _webSocket = webSocket;
        _textGenFactory = textGenFactory;
        _pendingSpeech = pendingSpeech;
        _animSelectFactory = animSelectFactory;
        _logger = logger;
        _bots = bots;
        _profile = profile;
    }

    private static string ProcessText(BotDefinition bot, ProfileOptions profile, string text) => text
        .Replace("{{Now}}", DateTime.Now.ToString("f", CultureInfo.InvariantCulture))
        .Replace("{{Bot}}", bot.Name)
        .Replace("{{User}}", profile.Name)
        .Replace("{{UserDescription}}", profile.Description.Trim(' ', '\r', '\n'))
        .Trim(' ', '\r', '\n');
    
    public async Task HandleWebSocketConnectionAsync(CancellationToken cancellationToken)
    {   
        var buffer = new byte[1024 * 4];

        var bots = await _bots.GetBotsListAsync(cancellationToken);
        await SendAsync(new ServerBotsListMessage
        {
            Bots = bots
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
            _logger.LogInformation("Cleared bot selection: {BotId}", selectBotMessage.BotId);
            return;
        }

        var bot = await _bots.GetBotAsync(selectBotMessage.BotId, cancellationToken);
        
        if (bot == null)
        {
            _logger.LogWarning("Received invalid bot selection: {BotId}", selectBotMessage.BotId);
            await SendAsync(new ServerErrorMessage
            {
                Message = $"Unknown bot {selectBotMessage.BotId}"
            }, cancellationToken);
            return;
        }

        _bot = bot;
        _logger.LogInformation("Selected bot: {BotId}", selectBotMessage.BotId);

        var textGen = _textGenFactory.Create(bot.Services.TextGen.Service);
        
        // TODO: Use a real chat data store, reload using auth
        var chatData = new ChatData
        {
            Id = Crypto.CreateCryptographicallySecureGuid(),
            UserName = _profile.Value.Name,
            BotName = bot.Name,
            Preamble = new TextData
            {
                Text = ProcessText(bot, _profile.Value, string.Join('\n', bot.Preamble))
            },
            Postamble = new TextData
            {
                Text = ProcessText(bot, _profile.Value, string.Join('\n', bot.Postamble))
            }
        };
        chatData.Preamble.Tokens = textGen.GetTokenCount(chatData.Preamble);
        foreach (var message in bot.Messages)
        {
            chatData.Messages.Add(
                new ChatMessageData
                {
                    User = message.User switch
                    {
                        "{{User}}" => _profile.Value.Name,
                        "{{Bot}}" => bot.Name,
                        _ => bot.Name
                    },
                    Text = ProcessText(bot, _profile.Value, message.Text)
                });
        }
        _chatData = chatData;
        await SendAsync(new ServerReadyMessage(), cancellationToken);
    }

    private async Task HandleClientMessage(ClientSendMessage sendMessage, CancellationToken cancellationToken)
    {
        if (_chatData is null || _bot == null)
        {
            await SendAsync(new ServerErrorMessage { Message = "Please select a bot first." }, cancellationToken);
            return;
        }
        
        _logger.LogInformation("Received chat message: {Text}", sendMessage.Text);
        // TODO: Save into some storage
        _chatData.Messages.Add(new ChatMessageData
        {
            Id = Guid.NewGuid(),
            User = _chatData.UserName,
            Timestamp = DateTimeOffset.UtcNow,
            Text = sendMessage.Text,
        });

        var textGen = _textGenFactory.Create(_bot.Services.TextGen.Service);
        var gen = await textGen.GenerateReplyAsync(_chatData);
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
            Service = _bot.Services.SpeechGen.Service,
            Text = gen.Text,
            Voice = _bot.Services.SpeechGen.Settings.TryGetValue("Voice", out var voice) ? voice : "Default"
        });
        await SendAsync(new ServerReplyMessage
        {
            Text = reply.Text,
            SpeechUrl = $"/chats/{_chatData.Id}/messages/{reply.Id}/speech/{_chatData.Id}_{reply.Id}.wav"
            
        }, cancellationToken);

        if (_animSelectFactory.TryCreate(_bot.Services.AnimSelect.Service, out var animSelect))
        {
            var animation = await animSelect.SelectAnimationAsync(_chatData);
            _logger.LogInformation("Selected animation: {Animation}", animation);
            await SendAsync(new ServerAnimationMessage { Value = animation }, cancellationToken);
        }
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
