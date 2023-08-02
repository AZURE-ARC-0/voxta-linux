using System.Net.WebSockets;
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

public sealed partial class UserConnection : IUserConnection
{
    private readonly IUserConnectionTunnel _tunnel;
    private readonly IProfileRepository _profileRepository;
    private readonly ICharacterRepository _charactersRepository;
    private readonly IChatRepository _chatRepository;
    private readonly ChatSessionFactory _chatSessionFactory;
    private readonly IUserConnectionManager _userConnectionManager;
    private readonly ILogger<UserConnection> _logger;

    private IChatSession? _chat;


    public string ConnectionId { get; } = Crypto.CreateCryptographicallySecureGuid().ToString();

    public UserConnection(IUserConnectionTunnel tunnel,
        IUserConnectionManager userConnectionManager,
        IProfileRepository profileRepository,
        ICharacterRepository charactersRepository,
        IChatRepository chatRepository,
        ChatSessionFactory chatSessionFactory,
        ILoggerFactory loggerFactory)
    {
        _tunnel = tunnel;
        _userConnectionManager = userConnectionManager;
        _profileRepository = profileRepository;
        _charactersRepository = charactersRepository;
        _chatSessionFactory = chatSessionFactory;
        _chatRepository = chatRepository;
        _logger = loggerFactory.CreateLogger<UserConnection>();
        _userConnectionManager.Register(this);
    }

    public async Task HandleWebSocketConnectionAsync(CancellationToken cancellationToken)
    {
        await _tunnel.SendAsync(new ServerWelcomeMessage
        {
            Username = (await _profileRepository.GetProfileAsync(cancellationToken))?.Name ?? "User",
        }, cancellationToken);

        while (!_tunnel.Closed)
        {
            try
            {
                var clientMessage = await _tunnel.ReceiveAsync<ClientMessage>(cancellationToken);
                if (clientMessage == null) return;

                switch (clientMessage)
                {
                    case ClientNewChatMessage newChatMessage:
                        await HandleNewChatAsync(newChatMessage, cancellationToken);
                        break;
                    case ClientStartChatMessage startChatMessage:
                        await HandleStartChatAsync(startChatMessage, cancellationToken);
                        break;
                    case ClientResumeChatMessage resumeChatMessage:
                        await HandleResumeChatAsync(resumeChatMessage, cancellationToken);
                        break;
                    case ClientStopChatMessage:
                        if (_chat != null) await _chat.DisposeAsync();
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
                    case ClientLoadCharactersListMessage:
                        await LoadCharactersListAsync(cancellationToken);
                        break;
                    case ClientLoadChatsListMessage loadChatsListMessage:
                        await LoadChatsListAsync(loadChatsListMessage.CharacterId, cancellationToken);
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
            catch (WebSocketException exc)
            {
                _logger.LogInformation("Disconnected by websocket abort: {Reason}", exc.Message);
                return;
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

    private async Task LoadCharactersListAsync(CancellationToken cancellationToken)
    {
        var characters = await _charactersRepository.GetCharactersListAsync(cancellationToken);
        await _tunnel.SendAsync(new ServerCharactersListLoadedMessage
        {
            Characters = characters,
        }, cancellationToken);
    }

    private async Task LoadChatsListAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var chats = await _chatRepository.GetChatsListAsync(characterId, cancellationToken);
        await _tunnel.SendAsync(new ServerChatsListLoadedMessage
        {
            Chats = chats
                .Select(c => new ServerChatsListLoadedMessage.ChatsListItem
                {
                    Id = c.Id
                })
                .ToArray(),
        }, cancellationToken);
    }

    private async Task LoadCharacterAsync(Guid characterId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading character {CharacterId}", characterId);

        var character = await _charactersRepository.GetCharacterAsync(characterId, cancellationToken);
        if (character == null)
        {
            await SendError("This character does not exist", cancellationToken);
            return;
        }

        await _tunnel.SendAsync(new ServerCharacterLoadedMessage
        {
            Character = character
        }, cancellationToken);
    }

    private Task SendError(string message, CancellationToken cancellationToken)
    {
        return _tunnel.SendAsync(new ServerErrorMessage { Message = message }, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        _userConnectionManager.Unregister(this);
        if (_chat != null) await _chat.DisposeAsync();
    }
}
