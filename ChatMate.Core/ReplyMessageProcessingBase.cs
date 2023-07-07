using ChatMate.Abstractions.Management;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Network;
using ChatMate.Common;

namespace ChatMate.Core;

public abstract class ReplyMessageProcessingBase
{
#warning It might be better to just inline it again
    private readonly IUserConnectionTunnel _tunnel;
    private readonly ChatSessionData _chatSessionData;
    private readonly ChatSessionState _chatSessionState;
    private readonly ClientStartChatMessage _startChatMessage;
    private readonly ChatServices _services;
    private readonly PendingSpeechManager _pendingSpeech;
    private readonly ITemporaryFileCleanup _temporaryFileCleanup;

    protected ReplyMessageProcessingBase(IUserConnectionTunnel tunnel, ChatSessionData chatSessionData, ChatSessionState chatSessionState, ClientStartChatMessage startChatMessage, ChatServices services, PendingSpeechManager pendingSpeech, ITemporaryFileCleanup temporaryFileCleanup)
    {
        _tunnel = tunnel;
        _chatSessionData = chatSessionData;
        _chatSessionState = chatSessionState;
        _startChatMessage = startChatMessage;
        _services = services;
        _pendingSpeech = pendingSpeech;
        _temporaryFileCleanup = temporaryFileCleanup;
    }

    protected async Task SendReply(ChatMessageData reply, CancellationToken cancellationToken)
    {
        var speechTask = CreateSpeechAsync(reply.Text, $"msg_{_chatSessionData.ChatId.ToString()}_{reply.Id}", out var speechUrl, cancellationToken);

        await _tunnel.SendAsync(new ServerReplyMessage
        {
            Text = reply.Text,
        }, cancellationToken);

        await speechTask;
        await _tunnel.SendAsync(new ServerSpeechMessage
        {
            Url = speechUrl,
        }, cancellationToken);

#warning Re-enable this but not as a bot option
        /*
        if (_servicesLocator.AnimSelectFactory.TryCreate(bot.Services.AnimSelect.Service, out var animSelect))
        {
            var animation = await animSelect.SelectAnimationAsync(chatData);
            _logger.LogInformation("Selected animation: {Animation}", animation);
            await _tunnel.SendAsync(new ServerAnimationMessage { Value = animation }, cancellationToken);
        }
        */
    }

    protected Task CreateSpeechAsync(string text, string id, out string speechUrl, CancellationToken cancellationToken)
    {
#warning Split in a separate class
        var ttsService = _startChatMessage.TtsService;
        var ttsVoice = _startChatMessage.TtsVoice;
        if (_services.TextToSpeech == null || string.IsNullOrEmpty(ttsVoice) || string.IsNullOrEmpty(ttsService))
        {
            speechUrl = "";
            return Task.CompletedTask;
        }

        Task speechTask;
        if (_startChatMessage.AudioPath != null)
        {
            speechUrl = Path.Combine(_startChatMessage.AudioPath, $"{id}.wav");
            if (!File.Exists(speechUrl))
            {
                _temporaryFileCleanup.MarkForDeletion(speechUrl);
                speechTask = _services.TextToSpeech.GenerateSpeechAsync(new SpeechRequest
                    {
                        Service = ttsService,
                        Text = text,
                        Voice = ttsVoice,
                    },
                    new FileSpeechTunnel(speechUrl),
                    "wav",
                    cancellationToken
                );
            }
            else
            {
                speechTask = Task.CompletedTask;
            }
        }
        else
        {
            speechUrl = CreateSpeechUrl(Crypto.CreateCryptographicallySecureGuid().ToString(), text, ttsService, ttsVoice);
            speechTask = Task.CompletedTask;
        }

        return speechTask;
    }

    private string CreateSpeechUrl(string id, string text, string ttsService, string ttsVoice)
    {
        _pendingSpeech.Push(id, new SpeechRequest
        {
            Service = ttsService,
            Text = text,
            Voice = ttsVoice,
        });
        var speechUrl = $"/tts/{id}.wav";
        return speechUrl;
    }

    protected ChatMessageData CreateMessageFromGen(TextData gen)
    {
        var reply = new ChatMessageData
        {
            Id = Guid.NewGuid(),
            User = _chatSessionData.BotName,
            Timestamp = DateTimeOffset.UtcNow,
            Text = gen.Text,
            Tokens = gen.Tokens,
        };
        // TODO: Save into some storage
        _chatSessionData.Messages.Add(reply);
        return reply;
    }
}