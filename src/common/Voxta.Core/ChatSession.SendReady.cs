using Voxta.Abstractions.Model;
using Voxta.Common;
using Microsoft.Extensions.Logging;

namespace Voxta.Core;

public partial class ChatSession
{
    public void SendReady()
    {
        Enqueue(SendReadyAsync);
    }

    private async ValueTask SendReadyAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Beginning chat with {CharacterName} with ID: {ChatId}", _chatSessionData.Character.Name, _chatSessionData.ChatId);
        _logger.LogInformation("Tex Gen: {ServiceName}", _textGen.ServiceName);
        _logger.LogInformation("Text To Speech: {ServiceName} (voice: {Voice})", _speechGenerator.ServiceName, _speechGenerator.Voice);
        _logger.LogInformation("Action Inference: {ServiceName}", _actionInference?.ServiceName ?? "None");
        _logger.LogInformation("Speech To Text: {ServiceName}", _speechToText?.ServiceName ?? "None");
        
        var thinkingSpeechUrls = new List<string>(_chatSessionData.ThinkingSpeech?.Length ?? 0);
        if (_chatSessionData.ThinkingSpeech != null)
        {
            foreach (var thinkingSpeech in _chatSessionData.ThinkingSpeech)
            {
                var thinkingSpeechId = Crypto.CreateSha1Hash($"{_chatSessionData.TtsVoice}::{thinkingSpeech}");
                var thinkingSpeechUrl = await _speechGenerator.CreateSpeechAsync(thinkingSpeech, thinkingSpeechId, true, cancellationToken);
                if (thinkingSpeechUrl != null)
                    thinkingSpeechUrls.Add(thinkingSpeechUrl);
            }
        }

        await _tunnel.SendAsync(new ServerReadyMessage
        {
            ChatId = _chatSessionData.ChatId,
            ThinkingSpeechUrls = thinkingSpeechUrls.ToArray(),
                
        }, cancellationToken);
        
        _logger.LogInformation("Chat ready!");

        if (_chatSessionData.Character.FirstMessage != null)
        {
            var textData = new TextData
            {
                Text = _chatSessionData.Character.FirstMessage,
                Tokens = _textGen.GetTokenCount(_chatSessionData.Character.FirstMessage)
            };
            var reply = ChatMessageData.FromGen(_chatSessionData.Character.Name, textData);
            _chatSessionData.Messages.Add(reply);
            _logger.LogInformation("Sending first message: {Message}", reply.Text);
            // generate sha1 hash from text and voice name to avoid duplicate speech generation
            var speechId = Crypto.CreateSha1Hash($"{_chatSessionData.TtsVoice}::{reply.Text}");
            var speechTask = Task.Run(() => _speechGenerator.CreateSpeechAsync(reply.Text, speechId, true, cancellationToken), cancellationToken);

            await _tunnel.SendAsync(new ServerReplyMessage
            {
                Text = reply.Text,
            }, cancellationToken);

            var speechUrl = await speechTask;
            if (speechUrl != null)
            {
                if (_pauseSpeechRecognitionDuringPlayback) _speechToText?.StopMicrophoneTranscription();
                await _tunnel.SendAsync(new ServerSpeechMessage
                {
                    Url = speechUrl,
                }, cancellationToken);
            }
        }
    }
}