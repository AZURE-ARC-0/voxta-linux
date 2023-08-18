using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;

namespace Voxta.Services.Mocks;

public class MockSummarizationService : MockServiceBase, ISummarizationService
{
    public int SummarizationDigestTokens => throw new NotSupportedException();

    public MockSummarizationService(ISettingsRepository settingsRepository) : base(settingsRepository)
    {
    }

    public ValueTask<string> SummarizeAsync(IChatInferenceData chat, IReadOnlyList<ChatMessageData> messagesToSummarize, CancellationToken cancellationToken)
    {
        using var token = chat.GetReadToken();
        return new ValueTask<string>($"Chat had {token.Messages.Count} messages.");
    }
}
