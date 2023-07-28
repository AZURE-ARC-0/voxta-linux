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

        #warning This should be moved to a separate method.
        var supportedExtensions = new[] { ".m4a", ".wav", ".mp3", ".webm" };
        if (Directory.Exists(@"Data\Audio\ThinkingSpeech\Shared"))
        {
            foreach (var file in Directory.GetFiles(@"Data\Audio\ThinkingSpeech\Shared"))
            {
                if (!supportedExtensions.Contains(Path.GetExtension(file).ToLower())) continue;
                var thinkingSpeechId = Crypto.CreateSha1Hash($"{file}");
                var thinkingSpeechUrl = await _speechGenerator.LoadSpeechAsync(file, thinkingSpeechId, true, cancellationToken);
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
            await SendReusableReplyWithSpeechAsync(reply.Text, cancellationToken);
        }
    }
}