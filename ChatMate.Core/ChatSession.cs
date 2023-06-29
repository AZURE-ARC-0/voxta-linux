using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Common;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class ChatSession
{
    private readonly IChatSessionTunnel _tunnel;
    private readonly ChatServicesLocator _servicesLocator;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ChatSession> _logger;

    private ChatInstance? _chat;

    public ChatSession(IChatSessionTunnel tunnel, ILoggerFactory loggerFactory, ChatServicesLocator servicesLocator)
    {
        _tunnel = tunnel;
        _servicesLocator = servicesLocator;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ChatSession>();
    }
    
    public async Task HandleWebSocketConnectionAsync(CancellationToken cancellationToken)
    {   
        var bots = await _servicesLocator.BotsRepository.GetBotsListAsync(cancellationToken);
        await _tunnel.SendAsync(new ServerWelcomeMessage
        {
            Bots = bots
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
                        await StartChatAsync(startChatMessage, cancellationToken);
                        break;
                    case ClientStopChatMessage:
                        _chat = null;
                        break;
                    case ClientSendMessage sendMessage:
                        if (_chat == null)
                        {
                            await _tunnel.SendAsync(new ServerErrorMessage { Message = "Please select a bot first." }, cancellationToken);
                            return;
                        }
                        await _chat.HandleMessageAsync(sendMessage, cancellationToken);
                        break;
                    case ClientListenMessage:
                        if (_chat == null)
                        {
                            await _tunnel.SendAsync(new ServerErrorMessage { Message = "Please select a bot first." }, cancellationToken);
                            return;
                        }
                        _chat.HandleListenAsync();
                        break;
                    default:
                        _logger.LogError("Unknown message type {ClientMessage}", clientMessage.GetType().Name);
                        break;
                }
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

    private async Task StartChatAsync(ClientStartChatMessage startChatMessage, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(startChatMessage.BotId))
        {
            _logger.LogWarning("Received empty bot selection");
            await _tunnel.SendAsync(new ServerErrorMessage
            {
                Message = $"Unknown bot {startChatMessage.BotId}"
            }, cancellationToken);
            return;
        }

        var bot = await _servicesLocator.BotsRepository.GetBotAsync(startChatMessage.BotId, cancellationToken);
        
        if (bot == null)
        {
            _logger.LogWarning("Received invalid bot selection: {BotId}", startChatMessage.BotId);
            await _tunnel.SendAsync(new ServerErrorMessage
            {
                Message = $"Unknown bot {startChatMessage.BotId}"
            }, cancellationToken);
            return;
        }

        _logger.LogInformation("Selected bot: {BotId}", startChatMessage.BotId);

        var profile = await _servicesLocator.ProfileRepository.GetProfileAsync() ?? new ProfileSettings { Name = "User", Description = "" };
        var textProcessor = new ChatTextProcessor(bot, profile);

        var textGen = _servicesLocator.TextGenFactory.Create(bot.Services.TextGen.Service);
        
        // TODO: Use a real chat data store, reload using auth
        var chatData = new ChatData
        {
            Id = Crypto.CreateCryptographicallySecureGuid(),
            UserName = profile.Name,
            BotName = bot.Name,
            Preamble = new TextData
            {
                Text = textProcessor.ProcessText(bot.Preamble)
            },
            Postamble = new TextData
            {
                Text = textProcessor.ProcessText(bot.Postamble)
            },
            Greeting = new TextData
            {
                Text = textProcessor.ProcessText(bot.Greeting)
            }
        };
        chatData.Preamble.Tokens = textGen.GetTokenCount(chatData.Preamble.Text);
        chatData.Postamble.Tokens = textGen.GetTokenCount(chatData.Postamble.Text);
        chatData.Greeting.Tokens = textGen.GetTokenCount(chatData.Greeting.Text);
        foreach (var message in bot.SampleMessages)
        {
            var m = new ChatMessageData
            {
                User = message.User switch
                {
                    "{{User}}" => profile.Name,
                    "{{Bot}}" => bot.Name,
                    _ => bot.Name
                },
                Text = textProcessor.ProcessText(message.Text)
            };
            m.Tokens = textGen.GetTokenCount(m.Text);
            chatData.SampleMessages.Add(m);
        }

        _chat = new ChatInstance(_tunnel, _loggerFactory, _servicesLocator, chatData, bot, textProcessor, startChatMessage.AudioPath);

        await _chat.SendReadyAsync(cancellationToken);
    }
}
