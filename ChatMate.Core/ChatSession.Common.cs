using ChatMate.Abstractions.Model;
using Microsoft.Extensions.Logging;

namespace ChatMate.Core;

public partial class ChatSession
{
    private async Task SendReplyWithSpeechAsync(ChatMessageData reply, string prefix, CancellationToken cancellationToken)
    {
        var speechTask = Task.Run(() => _speechGenerator.CreateSpeechAsync(reply.Text, $"{prefix}_{_chatSessionData.ChatId.ToString()}_{reply.Id}", false, cancellationToken), cancellationToken);

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

        if (_animationSelection != null && _chatSessionData.Actions is { Length: > 0 })
        {
            var animation = await _animationSelection.SelectActionAsync(_chatSessionData, cancellationToken);
            _logger.LogInformation("Selected animation: {Animation}", animation);
            await _tunnel.SendAsync(new ServerAnimationMessage { Value = animation }, cancellationToken);
        }
    }
}