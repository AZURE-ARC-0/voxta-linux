using Humanizer;
using Voxta.Abstractions.Model;
using Voxta.Common;
using Microsoft.Extensions.Logging;

namespace Voxta.Core;

public partial class ChatSession
{
    private static readonly string[] SupportedExtensions = { ".m4a", ".wav", ".mp3", ".webm" };
    
    public void HandleStartChat()
    {
        Enqueue(HandleStartChatAsync);
    }

    private async ValueTask HandleStartChatAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Beginning chat with {CharacterName}: {ChatId}", _chatSessionData.Character.Name, _chatSessionData.Chat.Id);
        _logger.LogInformation("Tex Gen: {ServiceName}", _textGen.ServiceName);
        _logger.LogInformation("Text To Speech: {ServiceName} (voice: {Voice})", _speechGenerator.ServiceName, _speechGenerator.Voice);
        _logger.LogInformation("Action Inference: {ServiceName}", _actionInference?.ServiceName ?? "None");
        _logger.LogInformation("Speech To Text: {ServiceName}", _speechToText?.ServiceName ?? "None");
        
        var thinkingSpeechUrls = new List<string>(_chatSessionData.ThinkingSpeech?.Length ?? 0);
        if (_chatSessionData.ThinkingSpeech != null)
        {
            foreach (var thinkingSpeech in _chatSessionData.ThinkingSpeech)
            {
                var thinkingSpeechId = Crypto.CreateSha1Hash($"{_speechGenerator.Voice ?? "NULL"}::{thinkingSpeech}");
                var thinkingSpeechUrl = await _speechGenerator.CreateSpeechAsync(thinkingSpeech, thinkingSpeechId, true, cancellationToken);
                if (thinkingSpeechUrl != null)
                    thinkingSpeechUrls.Add(thinkingSpeechUrl);
            }
        }
        await LoadThinkingSpeechFolder(@"Data\Audio\ThinkingSpeech\Shared", thinkingSpeechUrls, cancellationToken);

        await _tunnel.SendAsync(new ServerReadyMessage
        {
            ChatId = _chatSessionData.Chat.Id,
            Services = new CharacterServicesMap
            {
                TextGen = new ServiceMap
                {
                    Service = _textGen.ServiceName,
                },
                SpeechGen = new VoiceServiceMap
                {
                    Service = _speechGenerator.ServiceName,
                    Voice = _speechGenerator.Voice,
                },
                SpeechToText = new ServiceMap
                {
                    Service = _speechToText?.ServiceName ?? "",
                },
                ActionInference = new ServiceMap
                {
                    Service = _actionInference?.ServiceName ?? "",
                },
            },
            ThinkingSpeechUrls = thinkingSpeechUrls.ToArray(),
                
        }, cancellationToken);
        
        _logger.LogInformation("Chat ready!");

        await SendFirstMessageAsync(cancellationToken);
    }

    private async Task SendFirstMessageAsync(CancellationToken cancellationToken)
    {
        if (_chatSessionData.Messages.Count == 0 && _chatSessionData.Character.FirstMessage?.HasValue == true)
        {
            var reply = await AppendMessageAsync(_chatSessionData.Character.Name.Value, _chatSessionData.Character.FirstMessage);
            _logger.LogInformation("Sending first message: {Message}", reply.Value);
            await SendReusableReplyWithSpeechAsync(reply.Value, cancellationToken);
        }
        else
        {
            // TODO: Externalize these messages into a dedicated, localizable class
            var duration = DateTimeOffset.UtcNow - await GetLastUpdateAsync();
            HandleClientMessage(new ClientSendMessage
            {
                Text = $"[OOC: {_chatSessionData.User.Name} disconnects for {duration.Humanize()} and comes back online]"
            });
        }
    }

    private async Task<DateTimeOffset> GetLastUpdateAsync()
    {
        while (true)
        {
            if (_chatSessionData.Messages.Count == 0) return _chatSessionData.Chat.CreatedAt;
            #warning This is not tested, and should use a flag on the message instead of checking for the string.
            var lastMessage = _chatSessionData.Messages[^1];
            if (!lastMessage.Value.StartsWith("[OOC: ")) return lastMessage.Timestamp;

            _chatSessionData.Messages.Remove(lastMessage);
            await _chatMessageRepository.DeleteMessageAsync(lastMessage.Id);
        }
    }

    private async Task LoadThinkingSpeechFolder(string folder, ICollection<string> thinkingSpeechUrls, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(folder))
            return;

        foreach (var file in Directory.GetFiles(folder))
        {
            if (!SupportedExtensions.Contains(Path.GetExtension(file).ToLower())) continue;
            var thinkingSpeechId = Crypto.CreateSha1Hash($"{file}");
            var thinkingSpeechUrl = await _speechGenerator.LoadSpeechAsync(file, thinkingSpeechId, true, cancellationToken);
            if (thinkingSpeechUrl != null)
                thinkingSpeechUrls.Add(thinkingSpeechUrl);
        }
    }
}