using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public sealed class UserConnection : IAsyncDisposable
{
    private readonly IUserConnectionTunnel _tunnel;
    private readonly ICharacterRepository _charactersRepository;
    private readonly ChatSessionFactory _chatSessionFactory;
    private readonly ILogger<UserConnection> _logger;

    private IChatSession? _chat;

    public UserConnection(IUserConnectionTunnel tunnel, ILoggerFactory loggerFactory, ICharacterRepository charactersRepository, ChatSessionFactory chatSessionFactory)
    {
        _tunnel = tunnel;
        _charactersRepository = charactersRepository;
        _chatSessionFactory = chatSessionFactory;
        _logger = loggerFactory.CreateLogger<UserConnection>();
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
                    case ClientLoadCharacterMessage loadCharacterMessage:
                        await LoadCharacterAsync(loadCharacterMessage.CharacterId, cancellationToken);
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
            CharacterName = character.Name,
            Preamble = character.Preamble,
            Postamble = character.Postamble ?? "",
            Greeting = character.Greeting ?? "",
            SampleMessages = character.SampleMessages != null ?string.Join("\n", character.SampleMessages.Select(x => $"{x.User}: {x.Text}")) : "",
            TextGenService = character.Services.TextGen.Service,
            TtsService = character.Services.SpeechGen.Service,
            TtsVoice = character.Services.SpeechGen.Voice,
            EnableThinkingSpeech = character.Options?.EnableThinkingSpeech ?? true,
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
