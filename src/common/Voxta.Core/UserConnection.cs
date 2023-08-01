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

public sealed class UserConnection : IUserConnection
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
                        await StartChatAsync(newChatMessage, cancellationToken);
                        break;
                    case ClientStartChatMessage startChatMessage:
                        await StartChatAsync(startChatMessage, cancellationToken);
                        break;
                    case ClientResumeChatMessage resumeChatMessage:
                        await ResumeChatAsync(resumeChatMessage, cancellationToken);
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
            Name = character.Name,
            Description = character.Description,
            Personality = character.Personality,
            Scenario = character.Scenario,
            FirstMessage = character.FirstMessage,
            MessageExamples = character.MessageExamples ?? "",
            SystemPrompt = character.SystemPrompt,
            PostHistoryInstructions = character.PostHistoryInstructions,
            Culture = character.Culture,
            Prerequisites = character.Prerequisites != null ? string.Join(",", character.Prerequisites) : null,
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

    #warning Refactor those three functions
    private async Task StartChatAsync(ClientNewChatMessage newChatMessage, CancellationToken cancellationToken)
    {
        if(_chat != null) await _chat.DisposeAsync();
        _chat = null;
        
        if(!_userConnectionManager.TryGetChatLock(this))
        {
            await SendError("Another chat is in progress, close this one first.", cancellationToken);
            return;
        }
        
        var character = await _charactersRepository.GetCharacterAsync(newChatMessage.CharacterId, cancellationToken);
        if (character == null) throw new NullReferenceException($"Could not find character {newChatMessage.CharacterId}");
        if (newChatMessage.ClearExistingChats)
        {
            foreach (var c in await _chatRepository.GetChatsListAsync(newChatMessage.CharacterId, CancellationToken.None))
            {
                await _chatRepository.DeleteAsync(c.Id);
            }
        }
        var chat = new Chat
        {
            Id = Crypto.CreateCryptographicallySecureGuid(),
            CharacterId = character.Id,
        };
        await _chatRepository.SaveChatAsync(chat);

        _chat = await _chatSessionFactory.CreateAsync(_tunnel, chat, character, newChatMessage, cancellationToken);

        await _chatRepository.SaveChatAsync(chat);
        
        _logger.LogInformation("Started chat: {ChatId}", chat.Id);

        _chat.SendReady();
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
        
        var character = await _charactersRepository.GetCharacterAsync(startChatMessage.Character.Id, cancellationToken);
        if (character == null)
        {
            await _charactersRepository.SaveCharacterAsync(startChatMessage.Character);
            character = startChatMessage.Character;
        }

        Chat? chat = null;
        if (startChatMessage.ChatId != null)
        {
            chat = await _chatRepository.GetChatAsync(startChatMessage.ChatId.Value, cancellationToken);
        }
        if (chat == null)
        {
            chat = new Chat
            {
                Id = Crypto.CreateCryptographicallySecureGuid(),
                CharacterId = character.Id,
            };
            await _chatRepository.SaveChatAsync(chat);
        }

        _chat = await _chatSessionFactory.CreateAsync(_tunnel, chat, character, startChatMessage, cancellationToken);
        
        await _chatRepository.SaveChatAsync(chat);
        
        _logger.LogInformation("Started chat: {ChatId}", startChatMessage.ChatId);

        _chat.SendReady();
    }
    
    private async Task ResumeChatAsync(ClientResumeChatMessage resumeChatMessage, CancellationToken cancellationToken)
    {
        if(_chat != null) await _chat.DisposeAsync();
        _chat = null;
        
        if(!_userConnectionManager.TryGetChatLock(this))
        {
            await SendError("Another chat is in progress, close this one first.", cancellationToken);
            return;
        }
        
        var chat = await _chatRepository.GetChatAsync(resumeChatMessage.ChatId, cancellationToken);
        if (chat == null) throw new InvalidOperationException($"Chat {resumeChatMessage.ChatId} not found");
        var character = await _charactersRepository.GetCharacterAsync(chat.CharacterId, cancellationToken);
        if (character == null) throw new InvalidOperationException($"Character {chat.CharacterId} referenced in chat {resumeChatMessage.ChatId} was found");
        
        _chat = await _chatSessionFactory.CreateAsync(_tunnel, chat, character, resumeChatMessage, cancellationToken);
        
        _logger.LogInformation("Started chat: {ChatId}", resumeChatMessage.ChatId);

        _chat.SendReady();
    }

    public async ValueTask DisposeAsync()
    {
        _userConnectionManager.Unregister(this);
        if (_chat != null) await _chat.DisposeAsync();
    }
}
