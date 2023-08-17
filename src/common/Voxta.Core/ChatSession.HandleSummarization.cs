using Microsoft.Extensions.Logging;
using Voxta.Abstractions.Model;

namespace Voxta.Core;

public partial class ChatSession
{
    private async Task LaunchMemorySummarizationAsync(CancellationToken cancellationToken)
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

        #warning There is a risk of canceling here and having partially updated data. Use a transaction.
        var summaryId = Guid.NewGuid();
        messagesToSummarize.ForEach(m => m.SummarizedBy = summaryId);
        await Task.WhenAll(messagesToSummarize.Select(_chatMessageRepository.UpdateMessageAsync));
        messagesToSummarize.ForEach(m => _chatSessionData.Messages.Remove(m));

        var summarizedMessage = new ChatMessageData
        {
            Id = summaryId,
            Role = ChatMessageRole.System,
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