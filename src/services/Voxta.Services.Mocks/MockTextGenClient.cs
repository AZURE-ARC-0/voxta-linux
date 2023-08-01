using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;

namespace Voxta.Services.Mocks;

public class MockTextGenClient : ITextGenService
{
    public string ServiceName => MockConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW };
    
    public Task<bool> InitializeAsync(string[] prerequisites, string culture, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public ValueTask<string> GenerateReplyAsync(IReadOnlyChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult("This is a fake reply to: " + chatSessionData.GetMessages().LastOrDefault(x => x.User == chatSessionData.UserName)?.Text);
    }

    public int GetTokenCount(string message)
    {
        return 0;
    }

    public void Dispose()
    {
    }
}
