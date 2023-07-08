using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public sealed class UserConnection : IAsyncDisposable
{
    private readonly IUserConnectionTunnel _tunnel;
    private readonly ChatRepositories _repositories;
    private readonly ChatSessionFactory _chatSessionFactory;
    private readonly ILogger<UserConnection> _logger;

    private IChatSession? _chat;

    public UserConnection(IUserConnectionTunnel tunnel, ILoggerFactory loggerFactory, ChatRepositories repositories, ChatSessionFactory chatSessionFactory)
    {
        _tunnel = tunnel;
        _repositories = repositories;
        _chatSessionFactory = chatSessionFactory;
        _logger = loggerFactory.CreateLogger<UserConnection>();
    }
    
    public async Task HandleWebSocketConnectionAsync(CancellationToken cancellationToken)
    {   
        var bots = await _repositories.Bots.GetBotsListAsync(cancellationToken);
        await _tunnel.SendAsync(new ServerWelcomeMessage
        {
            BotTemplates = bots
        }, cancellationToken);

        while (!_tunnel.Closed)
        {

            try
            {
                var clientMessage = await _tunnel.ReceiveAsync<ClientMessage>(cancellationToken);
                if (clientMessage == null) return;

                switch (clientMessage)
                {
                    case ClientStartChatMessage startChatMessage:
                        if(_chat != null) await _chat.DisposeAsync();
                        _chat = null;
                        await StartChatAsync(startChatMessage, cancellationToken);
                        break;
                    case ClientStopChatMessage:
                        if(_chat != null) await _chat.DisposeAsync();
                        _chat = null;
                        break;
                    case ClientSendMessage sendMessage:
                        _chat?.HandleClientMessage(sendMessage);
                        break;
                    case ClientSpeechPlaybackStartMessage speechPlaybackStartMessage:
                        _chat?.HandleSpeechPlaybackStart(speechPlaybackStartMessage.Duration);
                        break;
                    case ClientSpeechPlaybackCompleteMessage:
                        _chat?.HandleSpeechPlaybackComplete();
                        break;
                    case ClientLoadBotTemplateMessage loadBotTemplateMessage:
                        await LoadBotTemplateAsync(loadBotTemplateMessage.BotTemplateId, cancellationToken);
                        break;
                    default:
                        _logger.LogError("Unknown message type {ClientMessage}", clientMessage.GetType().Name);
                        break;
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Disconnected by cancellation");
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "Error processing socket message");
                await _tunnel.SendAsync(new ServerErrorMessage
                {
                    Message = exc.Message,
                }, cancellationToken);
            }
        }
    }

    private async Task LoadBotTemplateAsync(string botTemplateId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading bot template {BotTemplateId}", botTemplateId);
        
        var bot = await _repositories.Bots.GetBotAsync(botTemplateId, cancellationToken);
        if (bot == null)
        {
            await SendError("This bot template does not exist", cancellationToken);
            return;
        }

        await _tunnel.SendAsync(new BotTemplateLoadedMessage
        {
            BotName = bot.Name,
            Preamble = bot.Preamble,
            Postamble = bot.Postamble ?? "",
            Greeting = bot.Greeting ?? "",
            SampleMessages = bot.SampleMessages != null ?string.Join("\n", bot.SampleMessages.Select(x => $"{x.User}: {x.Text}")) : "",
            TextGenService = bot.Services.TextGen.Service,
            TtsService = bot.Services.SpeechGen.Service,
            TtsVoice = bot.Services.SpeechGen.Voice,
        }, cancellationToken);
    }

    private Task SendError(string message, CancellationToken cancellationToken)
    {
        return _tunnel.SendAsync(new ServerErrorMessage { Message = message }, cancellationToken);
    }

    private async Task StartChatAsync(ClientStartChatMessage startChatMessage, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Started chat: {ChatId}", startChatMessage.ChatId);

        _chat = await _chatSessionFactory.CreateAsync(_tunnel, startChatMessage, cancellationToken);

        _chat.SendReady();
    }

    public async ValueTask DisposeAsync()
    {
        if (_chat != null) await _chat.DisposeAsync();
    }
}
