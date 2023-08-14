﻿using Voxta.Abstractions.Model;
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
        // Once 200 tokens are reached, summarize the older 100 tokens to a maximum 0f 50 tokens.
        #warning This is confusing; should these settings be in each summarizers? Or be a global, shared percentage?
        const int summarizeTriggerTokens = 160;
        const int tokensToSummarize = 120;
        const int summarizeToMaxTokens = 60;
        
        if (_chatSessionData.Messages.Sum(m => m.Tokens) < summarizeTriggerTokens) return;
        
        var messagesTokens = 0;
        var summaryId = Guid.NewGuid();
        var messagesToSummarize = new List<ChatMessageData>();
        foreach (var message in _chatSessionData.Messages)
        {
            if (messagesTokens + message.Tokens > tokensToSummarize) break;
            messagesTokens += message.Tokens;
            
            message.SummarizedBy = summaryId;
            messagesToSummarize.Add(message);
        }

        _logger.LogInformation("Summarizing memory for {MessagesToSummarizeCount}", messagesToSummarize.Count);

        if (messagesToSummarize.Count == 0)
            throw new InvalidOperationException("Cannot summarize, not enough tokens for a single message");

        var summaryText = await _summarizationService.SummarizeAsync(_chatSessionData, messagesToSummarize, cancellationToken);
        var summaryTokens = _textGen.GetTokenCount(summaryText);

        #warning There is a risk of canceling here and having partially updated data. Use a transaction.
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
