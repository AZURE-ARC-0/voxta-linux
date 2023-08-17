using Microsoft.Extensions.Logging;
using Voxta.Abstractions.Model;

namespace Voxta.Core;

public partial class ChatSession
{
    private readonly SemaphoreSlim _memorySummarizationSemaphore = new(1, 1);

    private async Task SummarizeMemoryAsync(CancellationToken cancellationToken)
    {
        if (_summarizationService == null) return;
        
        if (!await _memorySummarizationSemaphore.WaitAsync(0, cancellationToken))
            return;

        try
        {
            await SummarizeMemoryUnsafeAsync(cancellationToken);
        }
        finally
        {
            _memorySummarizationSemaphore.Release();
        }
    }

    private async Task SummarizeMemoryUnsafeAsync(CancellationToken cancellationToken)
    {
        if (_summarizationService == null) return;

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

        // TODO: Use a transaction here to reduce the risk of exiting while we write messages
        using var token = _chatSessionData.GetWriteToken();
        var summaryId = Guid.NewGuid();
        messagesToSummarize.ForEach(m => m.SummarizedBy = summaryId);
        await Task.WhenAll(messagesToSummarize.Select(_chatMessageRepository.UpdateMessageAsync));
        messagesToSummarize.ForEach(m => token.Messages.Remove(m));

        var summarizedMessage = new ChatMessageData
        {
            Id = summaryId,
            Role = ChatMessageRole.System,
            ChatId = token.Data.Id,
            Tokens = summaryTokens,
            Value = summaryText,
            // Adding a millisecond to preserve order
            Timestamp = messagesToSummarize.Last().Timestamp + TimeSpan.FromMilliseconds(1),
        };
        token.Messages.Insert(0, summarizedMessage);
        await SaveMessageAsync(summarizedMessage);

        _logger.LogInformation("Summarized memory (reduced from {MessageTokens} to {SummaryTokens}): {SummaryText}", messagesTokens, summaryTokens, summaryText);
    }
}