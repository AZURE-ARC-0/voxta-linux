using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;

namespace Voxta.Services.Mocks;

public class MockSummarizationService : ISummarizationService
{
    public string ServiceName => MockConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW };
    
    public Task<bool> TryInitializeAsync(string[] prerequisites, string culture, bool dry, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public ValueTask<string> SummarizeAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        return new ValueTask<string>($"Chat had {chat.GetMessages().Count} messages.");
    }

    public void Dispose()
    {
    }
}
