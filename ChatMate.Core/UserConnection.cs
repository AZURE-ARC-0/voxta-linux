using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Common;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class UserConnection : IDisposable
{
    private readonly IUserConnectionTunnel _tunnel;
    private readonly ChatServicesLocator _servicesLocator;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<UserConnection> _logger;

    private ChatInstance? _chat;

    public UserConnection(IUserConnectionTunnel tunnel, ILoggerFactory loggerFactory, ChatServicesLocator servicesLocator)
    {
        _tunnel = tunnel;
        _servicesLocator = servicesLocator;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<UserConnection>();
    }
    
    public async Task HandleWebSocketConnectionAsync(CancellationToken cancellationToken)
    {   
        var bots = await _servicesLocator.BotsRepository.GetBotsListAsync(cancellationToken);
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
                        _chat?.Dispose();
                        _chat = null;
                        await StartChatAsync(startChatMessage, cancellationToken);
                        break;
                    case ClientStopChatMessage:
                        _chat?.Dispose();
                        _chat = null;
                        break;
                    case ClientSendMessage sendMessage:
                        await (_chat?.HandleMessageAsync(sendMessage, cancellationToken) ?? SendError("Please select a bot first.", cancellationToken));
                        break;
                    #warning Better name
                    case ClientListenMessage:
                        await (_chat?.HandleListenAsync() ?? SendError("Please select a bot first.", cancellationToken));
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
        
        var bot = await _servicesLocator.BotsRepository.GetBotAsync(botTemplateId, cancellationToken);
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
            SampleMessages = string.Join("\n", bot.SampleMessages.Select(x => $"{x.User}: {x.Text}")),
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

        var profile = await _servicesLocator.ProfileRepository.GetProfileAsync() ?? new ProfileSettings { Name = "User", Description = "" };
        var textProcessor = new ChatTextProcessor(profile, startChatMessage.BotName);

        var textGen = _servicesLocator.TextGenFactory.Create(startChatMessage.TextGenService);
        
        // TODO: Use a real chat data store, reload using auth
        var chatData = new ChatSessionData
        {
            ChatId = startChatMessage.ChatId ?? Crypto.CreateCryptographicallySecureGuid(),
            UserName = profile.Name,
            BotName = startChatMessage.BotName,
            Preamble = new TextData
            {
                Text = textProcessor.ProcessText(startChatMessage.Preamble)
            },
            Postamble = new TextData
            {
                Text = textProcessor.ProcessText(startChatMessage.Postamble)
            },
            Greeting = !string.IsNullOrEmpty(startChatMessage.Greeting) ? new TextData
            {
                Text = textProcessor.ProcessText(startChatMessage.Greeting)
            } : null
        };
        chatData.Preamble.Tokens = textGen.GetTokenCount(chatData.Preamble.Text);
        chatData.Postamble.Tokens = textGen.GetTokenCount(chatData.Postamble.Text);
        if(chatData.Greeting != null) chatData.Greeting.Tokens = textGen.GetTokenCount(chatData.Greeting.Text);
        var sampleMessages = startChatMessage.SampleMessages?.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        foreach (var message in sampleMessages)
        {
            var parts = message.Split(":");
            if (parts.Length == 1) continue;
            var m = new ChatMessageData
            {
                User = parts[0] switch
                {
                    "{{User}}" => profile.Name,
                    "{{Bot}}" => startChatMessage.BotName,
                    _ => startChatMessage.BotName
                },
                Text = textProcessor.ProcessText(parts[1].Trim())
            };
            m.Tokens = textGen.GetTokenCount(m.Text);
            chatData.SampleMessages.Add(m);
        }

        _chat = new ChatInstance(
            _tunnel,
            _loggerFactory,
            _servicesLocator,
            chatData,
            startChatMessage,
            textProcessor,
            startChatMessage.AudioPath,
            startChatMessage.UseServerSpeechRecognition
        );

        await _chat.SendReadyAsync(cancellationToken);
    }

    public void Dispose()
    {
        _chat?.Dispose();
    }
}
