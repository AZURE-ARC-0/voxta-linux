using ChatMate.Abstractions.Model;
using ChatMate.Common;

namespace ChatMate.Core;

public partial class ChatSession
{
    private async Task SendReply(ChatMessageData reply, CancellationToken cancellationToken)
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
        _chatSessionState.SpeechStart();

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

    private Task CreateSpeechAsync(string text, string id, out string speechUrl, CancellationToken cancellationToken)
    {
        var ttsVoice = _chatSessionData.TtsVoice;
        if (_services.TextToSpeech == null || string.IsNullOrEmpty(ttsVoice))
        {
            speechUrl = "";
            return Task.CompletedTask;
        }

        Task speechTask;
        if (_chatSessionData.AudioPath != null)
        {
            speechUrl = Path.Combine(_chatSessionData.AudioPath, $"{id}.wav");
            if (!File.Exists(speechUrl))
            {
                _temporaryFileCleanup.MarkForDeletion(speechUrl);
                speechTask = _services.TextToSpeech.GenerateSpeechAsync(new SpeechRequest
                    {
                        Service = _services.TextToSpeech.ServiceName,
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
            speechUrl = CreateSpeechUrl(Crypto.CreateCryptographicallySecureGuid().ToString(), text, _services.TextToSpeech.ServiceName, ttsVoice);
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

    private ChatMessageData CreateMessageFromGen(TextData gen)
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