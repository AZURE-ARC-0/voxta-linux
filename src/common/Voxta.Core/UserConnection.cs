using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Voxta.Common;

namespace Voxta.Core;

public interface IUserConnection : IAsyncDisposable
{
    string ConnectionId { get; }
    Task HandleWebSocketConnectionAsync(CancellationToken cancellationToken);
}

public sealed class UserConnection : IUserConnection
{
    private readonly IUserConnectionTunnel _tunnel;
    private readonly ICharacterRepository _charactersRepository;
    private readonly ChatSessionFactory _chatSessionFactory;
    private readonly IUserConnectionManager _userConnectionManager;
    private readonly ILogger<UserConnection> _logger;

    private IChatSession? _chat;

    public string ConnectionId { get; } = Crypto.CreateCryptographicallySecureGuid().ToString();

    public UserConnection(IUserConnectionTunnel tunnel, ILoggerFactory loggerFactory, ICharacterRepository charactersRepository, ChatSessionFactory chatSessionFactory, IUserConnectionManager userConnectionManager)
    {
        _tunnel = tunnel;
        _charactersRepository = charactersRepository;
        _chatSessionFactory = chatSessionFactory;
        _userConnectionManager = userConnectionManager;
        _logger = loggerFactory.CreateLogger<UserConnection>();
        _userConnectionManager.Register(this);
    }
    
    public async Task HandleWebSocketConnectionAsync(CancellationToken cancellationToken)
    {   
        var characters = await _charactersRepository.GetCharactersListAsync(cancellationToken);
        await _tunnel.SendAsync(new ServerWelcomeMessage
        {
            Characters = characters
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
                        if(_chat != null) await _chat.DisposeAsync();
                        _chat = null;
                        _userConnectionManager.ReleaseChatLock(this);
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
                    case ClientLoadCharacterMessage loadCharacterMessage:
                        await LoadCharacterAsync(loadCharacterMessage.CharacterId, cancellationToken);
                        break;
                    default:
                        _logger.LogError("Unknown message type {ClientMessage}", clientMessage.GetType().Name);
                        break;
                }
            }
            catch (OperationCanceledException)
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

    private async Task LoadCharacterAsync(string characterId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading character {CharacterId}", characterId);
        
        var character = await _charactersRepository.GetCharacterAsync(characterId, cancellationToken);
        if (character == null)
        {
            await SendError("This character does not exist", cancellationToken);
            return;
        }

        await _tunnel.SendAsync(new CharacterLoadedMessage
        {
            Name = character.Name,
            Description = character.Description,
            Personality = character.Personality,
            Scenario = character.Scenario,
            FirstMessage = character.FirstMessage ?? "",
            MessageExamples = character.MessageExamples ?? "",
            SystemPrompt = character.SystemPrompt,
            PostHistoryInstructions = character.PostHistoryInstructions,
            Culture = character.Culture,
            TextGenService = character.Services.TextGen.Service ?? "",
            TtsService = character.Services.SpeechGen.Service ?? "",
            TtsVoice = character.Services.SpeechGen.Voice ?? "",
            EnableThinkingSpeech = character.Options?.EnableThinkingSpeech ?? true,
        }, cancellationToken);
    }

    private Task SendError(string message, CancellationToken cancellationToken)
    {
        return _tunnel.SendAsync(new ServerErrorMessage { Message = message }, cancellationToken);
    }

    private async Task StartChatAsync(ClientStartChatMessage startChatMessage, CancellationToken cancellationToken)
    {
        if(_chat != null) await _chat.DisposeAsync();
        _chat = null;
        
        if(!_userConnectionManager.TryGetChatLock(this))
        {
            await SendError("Another chat is in progress, close this one first.", cancellationToken);
            return;
        }
        
        _chat = await _chatSessionFactory.CreateAsync(_tunnel, startChatMessage, cancellationToken);
        
        _logger.LogInformation("Started chat: {ChatId}", startChatMessage.ChatId);

        _chat.SendReady();
    }

    public async ValueTask DisposeAsync()
    {
        _userConnectionManager.Unregister(this);
        if (_chat != null) await _chat.DisposeAsync();
    }
}
