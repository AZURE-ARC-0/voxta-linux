using System.Globalization;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Common;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class ChatSession
{
    private readonly IChatSessionTunnel _tunnel;
    private readonly ChatServicesFactory _servicesFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ChatSession> _logger;

    private ChatInstance? _chat;

    public ChatSession(IChatSessionTunnel tunnel, ILoggerFactory loggerFactory, ChatServicesFactory servicesFactory)
    {
        _tunnel = tunnel;
        _servicesFactory = servicesFactory;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ChatSession>();
    }

    private static string ProcessText(BotDefinition bot, ProfileSettings profile, string text) => text
        .Replace("{{Now}}", DateTime.Now.ToString("f", CultureInfo.InvariantCulture))
        .Replace("{{Bot}}", bot.Name)
        .Replace("{{User}}", profile.Name)
        .Replace("{{UserDescription}}", profile.Description?.Trim(' ', '\r', '\n') ?? "Not specified")
        .Trim(' ', '\r', '\n');
    
    public async Task HandleWebSocketConnectionAsync(CancellationToken cancellationToken)
    {   
        var bots = await _servicesFactory._bots.GetBotsListAsync(cancellationToken);
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
                    case ClientCreateChatMessage selectBotMessage:
                        await CreateChatAsync(selectBotMessage, cancellationToken);
                        break;
                    case ClientSendMessage sendMessage:
                        if (_chat == null)
                        {
                            await _tunnel.SendAsync(new ServerErrorMessage { Message = "Please select a bot first." }, cancellationToken);
                            return;
                        }
                        await _chat.HandleMessageAsync(sendMessage, cancellationToken);
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

    private async Task CreateChatAsync(ClientCreateChatMessage createChatMessage, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(createChatMessage.BotId))
        {
            _logger.LogInformation("Cleared bot selection: {BotId}", createChatMessage.BotId);
            return;
        }

        var bot = await _servicesFactory._bots.GetBotAsync(createChatMessage.BotId, cancellationToken);
        
        if (bot == null)
        {
            _logger.LogWarning("Received invalid bot selection: {BotId}", createChatMessage.BotId);
            await _tunnel.SendAsync(new ServerErrorMessage
            {
                Message = $"Unknown bot {createChatMessage.BotId}"
            }, cancellationToken);
            return;
        }

        _logger.LogInformation("Selected bot: {BotId}", createChatMessage.BotId);

        var textGen = _servicesFactory._textGenFactory.Create(bot.Services.TextGen.Service);
        
        // TODO: Use a real chat data store, reload using auth
        var profile = await _servicesFactory._profileRepository.GetProfileAsync() ?? new ProfileSettings { Name = "User", Description = "" };
        var chatData = new ChatData
        {
            Id = Crypto.CreateCryptographicallySecureGuid(),
            UserName = profile.Name,
            BotName = bot.Name,
            Preamble = new TextData
            {
                Text = ProcessText(bot, profile, string.Join('\n', bot.Preamble))
            },
            Postamble = new TextData
            {
                Text = ProcessText(bot, profile, string.Join('\n', bot.Postamble))
            },
            Greeting = new TextData
            {
                Text = ProcessText(bot, profile, string.Join('\n', bot.Greeting))
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
                Text = ProcessText(bot, profile, message.Text)
            };
            m.Tokens = textGen.GetTokenCount(m.Text);
            chatData.SampleMessages.Add(m);
        }

        _chat = new ChatInstance(_tunnel, _loggerFactory, _servicesFactory, chatData, bot, createChatMessage.AudioPath);

        await _chat.SendReadyAsync(cancellationToken);
    }
}
