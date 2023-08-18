using Voxta.Abstractions.Model;

namespace Voxta.Abstractions.Services;

public interface ISummarizationService : IService
{
    int SummarizationDigestTokens { get; }
    ValueTask<string> SummarizeAsync(IChatInferenceData chat, IReadOnlyList<ChatMessageData> messagesToSummarize, CancellationToken cancellationToken);
}