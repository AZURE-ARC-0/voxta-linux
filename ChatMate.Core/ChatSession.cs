using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ChatMate.Abstractions.DependencyInjection;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;
using ChatMate.Common;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public class ChatSessionFactory
{
    private readonly ISelectorFactory<ITextGenService> _textGenFactory;
    private readonly PendingSpeechManager _pendingSpeech;
    private readonly ISelectorFactory<IAnimationSelectionService> _animSelectFactory;
    private readonly ILogger<ChatSession> _logger;
    private readonly IBotRepository _bots;
    private readonly IProfileRepository _profileRepository;

    [SuppressMessage("ReSharper", "ContextualLoggerProblem")]
    public ChatSessionFactory(ISelectorFactory<ITextGenService> textGenFactory, PendingSpeechManager pendingSpeech, ISelectorFactory<IAnimationSelectionService> animSelectFactory, ILogger<ChatSession> logger, IBotRepository bots, IProfileRepository profileRepository)
    {
        _textGenFactory = textGenFactory;
        _pendingSpeech = pendingSpeech;
        _animSelectFactory = animSelectFactory;
        _logger = logger;
        _bots = bots;
        _profileRepository = profileRepository;
    }
    
    public ChatSession Create(IChatSessionTunnel tunnel)
    {
        return new ChatSession(tunnel, _textGenFactory, _pendingSpeech, _animSelectFactory, _logger, _bots, _profileRepository);
    }
}

public class ChatSession
{
    private readonly IChatSessionTunnel _tunnel;
    private readonly ISelectorFactory<ITextGenService> _textGenFactory;
    private readonly PendingSpeechManager _pendingSpeech;
    private readonly ISelectorFactory<IAnimationSelectionService> _animSelectFactory;
    private readonly ILogger<ChatSession> _logger;
    private readonly IBotRepository _bots;
    private readonly IProfileRepository _profileRepository;

    private BotDefinition? _bot;
    private ChatData? _chatData;

    public ChatSession(IChatSessionTunnel tunnel, ISelectorFactory<ITextGenService> textGenFactory, PendingSpeechManager pendingSpeech, ISelectorFactory<IAnimationSelectionService> animSelectFactory, ILogger<ChatSession> logger, IBotRepository bots, IProfileRepository profileRepository)
    {
        _tunnel = tunnel;
        _textGenFactory = textGenFactory;
        _pendingSpeech = pendingSpeech;
        _animSelectFactory = animSelectFactory;
        _logger = logger;
        _bots = bots;
        _profileRepository = profileRepository;
    }

    private static string ProcessText(BotDefinition bot, ProfileSettings profile, string text) => text
        .Replace("{{Now}}", DateTime.Now.ToString("f", CultureInfo.InvariantCulture))
        .Replace("{{Bot}}", bot.Name)
        .Replace("{{User}}", profile.Name)
        .Replace("{{UserDescription}}", profile.Description?.Trim(' ', '\r', '\n') ?? "Not specified")
        .Trim(' ', '\r', '\n');
    
    public async Task HandleWebSocketConnectionAsync(CancellationToken cancellationToken)
    {   
        var bots = await _bots.GetBotsListAsync(cancellationToken);
        await _tunnel.SendAsync(new ServerBotsListMessage
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
                    case ClientSelectBotMessage selectBotMessage:
                        await HandleSelectBotMessage(selectBotMessage, cancellationToken);
                        break;
                    case ClientSendMessage sendMessage:
                        await HandleClientMessage(sendMessage, cancellationToken);
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
            await _tunnel.SendAsync(new ServerErrorMessage
            {
                Message = $"Unknown bot {selectBotMessage.BotId}"
            }, cancellationToken);
            return;
        }

        _bot = bot;
        _logger.LogInformation("Selected bot: {BotId}", selectBotMessage.BotId);

        var textGen = _textGenFactory.Create(bot.Services.TextGen.Service);
        
        // TODO: Use a real chat data store, reload using auth
        var profile = await _profileRepository.GetProfileAsync() ?? new ProfileSettings { Name = "User", Description = "" };
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
        _chatData = chatData;

        await _tunnel.SendAsync(new ServerReadyMessage
        {
            BotId = bot.Name,
            ThinkingSpeechUrls =
                bot.ThinkingSpeech?.Select(x =>
                    CreateSpeechUrl(chatData.Id, bot, Guid.NewGuid(), x)
                ).ToArray() ?? Array.Empty<string>()
        }, cancellationToken);

        if (chatData.Greeting.HasValue)
        {
            await SendReply(chatData.BotName, chatData.Greeting, cancellationToken);
        }
    }

    private async Task HandleClientMessage(ClientSendMessage sendMessage, CancellationToken cancellationToken)
    {
        if (_chatData is null || _bot == null)
        {
            await _tunnel.SendAsync(new ServerErrorMessage { Message = "Please select a bot first." }, cancellationToken);
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
        await SendReply(_chatData.BotName, gen, cancellationToken);
    }

    private async Task SendReply(string botName, TextData gen, CancellationToken cancellationToken)
    {
        var chatData = _chatData;
        var bot = _bot;
        if (chatData == null || bot == null) throw new NullReferenceException("No active chat");
        
        var reply = new ChatMessageData
        {
            Id = Guid.NewGuid(),
            User = botName,
            Timestamp = DateTimeOffset.UtcNow,
            Text = gen.Text,
            Tokens = gen.Tokens,
        };
        _logger.LogInformation("Reply ({Tokens} tokens): {Text}", reply.Tokens, reply.Text);
        // TODO: Save into some storage
        chatData.Messages.Add(reply);
        var speechUrl = CreateSpeechUrl(chatData.Id, bot, reply.Id, gen.Text);
        await _tunnel.SendAsync(new ServerReplyMessage
        {
            Text = reply.Text,
            SpeechUrl = speechUrl,
        }, cancellationToken);

        if (_animSelectFactory.TryCreate(bot.Services.AnimSelect.Service, out var animSelect))
        {
            var animation = await animSelect.SelectAnimationAsync(chatData);
            _logger.LogInformation("Selected animation: {Animation}", animation);
            await _tunnel.SendAsync(new ServerAnimationMessage { Value = animation }, cancellationToken);
        }
    }

    private string CreateSpeechUrl(Guid chatId, BotDefinition bot, Guid messageId, string text)
    {
        _pendingSpeech.Push(chatId, messageId, new SpeechRequest
        {
            Service = bot.Services.SpeechGen.Service,
            Text = text,
            Voice = bot.Services.SpeechGen.Settings.TryGetValue("Voice", out var voice) ? voice : "Default"
        });
        var speechUrl = $"/chats/{chatId}/messages/{messageId}/speech/{chatId}_{messageId}.wav";
        return speechUrl;
    }
}
