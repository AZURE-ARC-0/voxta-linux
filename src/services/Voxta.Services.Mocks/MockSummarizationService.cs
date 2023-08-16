using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;

namespace Voxta.Services.Mocks;

public class MockSummarizationService : MockServiceBase, ISummarizationService
{
    public MockSummarizationService(ISettingsRepository settingsRepository) : base(settingsRepository)
    {
    }

    public ValueTask<string> SummarizeAsync(IChatInferenceData chat, List<ChatMessageData> messagesToSummarize, CancellationToken cancellationToken)
    {
        return new ValueTask<string>($"Chat had {chat.GetMessages().Count} messages.");
    }
}
