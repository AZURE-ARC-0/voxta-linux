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

        if (_actionInference != null && _chatSessionData.Actions is { Length: > 0 })
        {
            var action = await _actionInference.SelectActionAsync(_chatSessionData, cancellationToken);
            if (!_chatSessionData.Actions.Contains(action))
            {
                var incorrect = action;
                var potentialActions = _chatSessionData.Actions
                    .Select(x => (distance: incorrect.GetLevenshteinDistance(x), value: x))
                    .Where(x => x.distance <= 3)
                    .ToArray();
                // ReSharper disable once PatternIsRedundant
                var replaced = potentialActions switch
                {
                    { Length: 0 } => "",
                    { Length: 1 } => potentialActions[0].value,
                    { Length: > 1 } => potentialActions.MinBy(x => x.distance).value,
                    _ => throw new NotSupportedException($"Unexpected potential actions length {potentialActions.Length}")
                };
                _logger.LogInformation("Selected action: '{GuessedAction}' based on approximation from {Action} with list {Actions}", replaced, action, _chatSessionData.Actions);
                action = replaced;
            }
            else
            {
                _logger.LogInformation("Selected action: {Action}", action);
            }

            await _tunnel.SendAsync(new ServerActionMessage { Value = action }, cancellationToken);
        }
    }
}