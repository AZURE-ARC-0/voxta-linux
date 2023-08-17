using Voxta.Abstractions.Model;
using Microsoft.Extensions.Logging;

namespace Voxta.Core;

public partial class ChatSession
{
    private const int MaxEmptyRepliesRetries = 3;

    private async ValueTask GenerateReplyAsync(ClientSendMessage clientSendMessage, CancellationToken abortCancellationToken, CancellationToken queueCancellationToken)
    {
        var text = clientSendMessage.Text;

        var speechInterruptionRatio = _chatSessionState.InterruptSpeech();
        
        if (speechInterruptionRatio is > 0.05f and < 0.95f)
        {
            var lastCharacterMessage = _chatSessionData.Messages.LastOrDefault();
            if (lastCharacterMessage?.User == _chatSessionData.Character.Name.Value)
            {
                var cutoff = Math.Clamp((int)Math.Round(lastCharacterMessage.Value.Length * speechInterruptionRatio), 1, lastCharacterMessage.Value.Length - 2);
                lastCharacterMessage.Value = lastCharacterMessage.Value[..cutoff] + "...";
                lastCharacterMessage.Tokens = _textGen.GetTokenCount(lastCharacterMessage.Value);
                _logger.LogInformation("Cutoff last character message to account for the interruption: {Text}", lastCharacterMessage.Value);
            }

            text =$" *interrupts {_chatSessionData.Character.Name}* " + text;
            _logger.LogInformation("Added interruption notice to the user message. Updated text: {Text}", text);
        }

        if (_chatSessionState.PendingUserMessage == null)
        {
            var userText = _chatTextProcessor.ProcessText(text);
            var userMessageData = await AppendMessageAsync(_chatSessionData.User, userText);
            _chatSessionState.PendingUserMessage = userMessageData;
        }
        else
        {
            var append = "; " + text;
            _chatSessionState.PendingUserMessage.Value += append;
            _chatSessionState.PendingUserMessage.Tokens += _textGen.GetTokenCount(append);
            await UpdateMessageAsync(_chatSessionState.PendingUserMessage);
        }

        _chatSessionData.Actions = clientSendMessage.Actions;
        _chatSessionData.Context = _chatTextProcessor.ProcessText(clientSendMessage.Context);

        string? generated = null;
        try
        {
            using var linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(queueCancellationToken, abortCancellationToken);
            var linkedCancellationToken = linkedCancellationSource.Token;
            _memoryProvider.QueryMemoryFast(_chatSessionData);
            linkedCancellationSource.Token.ThrowIfCancellationRequested();
            for (var i = 0; i < MaxEmptyRepliesRetries; i++)
            {
                generated = await _textGen.GenerateReplyAsync(_chatSessionData, linkedCancellationToken);
                if (string.IsNullOrWhiteSpace(generated))
                {
                    _logger.LogWarning("Empty reply generated. Attempt {Attempt} / {MaxEmptyRepliesRetries}...", i, MaxEmptyRepliesRetries);
                    continue;
                }
                generated = _sanitizer.Sanitize(generated);
                if (!string.IsNullOrWhiteSpace(generated)) break;
            }
            if (string.IsNullOrWhiteSpace(generated))
            {
                throw new InvalidOperationException("AI service returned an empty string.");
            }
        }
        catch (OperationCanceledException)
        {
            // Reply will simply be dropped
            return;
        }
        catch
        {
            if (_pauseSpeechRecognitionDuringPlayback) _speechToText?.StartMicrophoneTranscription();
            throw;
        }

        _chatSessionState.PendingUserMessage = null;
        var genData = new TextData { Value = generated, Tokens = _textGen.GetTokenCount(generated) };
        var reply = await AppendMessageAsync(_chatSessionData.Character, genData);

        await LaunchMemorySummarizationAsync(queueCancellationToken);
        
        _logger.LogInformation("{Character} replied with: {Text}", _chatSessionData.Character.Name, reply.Value);

        var speechId = $"speech_{_chatSessionData.Chat.Id}_{reply.Id}";
        await SendReplyWithSpeechAsync(reply.Value, speechId, false, queueCancellationToken);
        await GenerateActionInference(queueCancellationToken);
    }
}
