using Voxta.Abstractions.Model;
using Microsoft.Extensions.Logging;

namespace Voxta.Core;

public partial class ChatSession
{
    private async Task SendReplyWithSpeechAsync(TextData reply, string speechId, CancellationToken cancellationToken)
    {
        var speechTask = Task.Run(() => _speechGenerator.CreateSpeechAsync(reply.Text, speechId, false, cancellationToken), cancellationToken);

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
            var action = await _animationSelection.SelectActionAsync(_chatSessionData, cancellationToken);
            if (!_chatSessionData.Actions.Contains(action))
            {
                var incorrect = action;
                action = _chatSessionData.Actions
                    .Select(x => (distance: incorrect.GetLevenshteinDistance(x), value: x))
                    .Where(x => x.distance <= 3)
                    .MinBy(x => x.distance)
                    .value ?? "idle";
            }
            _logger.LogInformation("Selected action: {Animation}", action);
            await _tunnel.SendAsync(new ServerActionMessage { Value = action }, cancellationToken);
        }
    }
}