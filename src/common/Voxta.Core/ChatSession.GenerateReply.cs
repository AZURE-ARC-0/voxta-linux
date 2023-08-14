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

            text = "*interrupts {{char}}* " + text;
            _logger.LogInformation("Added interruption notice to the user message. Updated text: {Text}", text);
        }

        if (_chatSessionState.PendingUserMessage == null)
        {
            var userText = _chatTextProcessor.ProcessText(text);
            var userMessageData = await AppendMessageAsync(_chatSessionData.User.Name.Value, userText);
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
        var reply = await AppendMessageAsync(_chatSessionData.Character.Name.Value, genData);

        await LaunchMemorySummarizationAsync(queueCancellationToken);
        
        _logger.LogInformation("{Character} replied with: {Text}", _chatSessionData.Character.Name, reply.Value);

        var speechId = $"speech_{_chatSessionData.Chat.Id}_{reply.Id}";
        await SendReplyWithSpeechAsync(reply.Value, speechId, false, queueCancellationToken);
        await GenerationActionInference(queueCancellationToken);
    }

    private async Task LaunchMemorySummarizationAsync(CancellationToken cancellationToken)
    {
        var summarizePlan = _textGen.GetMessagesToSummarize(_chatSessionData);

        if (summarizePlan == null) return;
        
        var (messagesToSummarize, messagesTokens) = summarizePlan.Value;
        
        _logger.LogInformation("Summarizing memory for {MessagesToSummarizeCount}", messagesToSummarize.Count);

        var summaryText = await _summarizationService.SummarizeAsync(_chatSessionData, messagesToSummarize, cancellationToken);
        if (string.IsNullOrEmpty(summaryText))
        {
            _logger.LogWarning("Empty summarization returned from the service");
            return;
        }
        summaryText = _sanitizer.StripUnfinishedSentence(summaryText);
        var summaryTokens = _textGen.GetTokenCount(summaryText);

        #warning There is a risk of canceling here and having partially updated data. Use a transaction.
        var summaryId = Guid.NewGuid();
        messagesToSummarize.ForEach(m => m.SummarizedBy = summaryId);
        await Task.WhenAll(messagesToSummarize.Select(_chatMessageRepository.UpdateMessageAsync));
        messagesToSummarize.ForEach(m => _chatSessionData.Messages.Remove(m));

        #warning Use known user names, maybe flags, instead of this?
        var summarizedMessage = new ChatMessageData
        {
            Id = summaryId,
            User = "System",
            ChatId = _chatSessionData.Chat.Id,
            Tokens = summaryTokens,
            Value = summaryText,
            // Adding a millisecond to preserve order
            Timestamp = messagesToSummarize.Last().Timestamp + TimeSpan.FromMilliseconds(1),
        };
        _chatSessionData.Messages.Insert(0, summarizedMessage);
        await SaveMessageAsync(summarizedMessage);

        _logger.LogInformation("Summarized memory (reduced from {MessageTokens} to {SummaryTokens}): {SummaryText}", messagesTokens, summaryTokens, summaryText);
    }
}
