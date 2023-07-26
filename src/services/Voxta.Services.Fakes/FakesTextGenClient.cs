using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;

namespace Voxta.Services.Fakes;

public class FakesTextGenClient : ITextGenService
{
    public Task<bool> InitializeAsync(string[] prerequisites, string culture, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public ValueTask<TextData> GenerateReplyAsync(IReadOnlyChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new TextData
        {
            Text = "This is a fake reply to: " + chatSessionData.GetMessages().LastOrDefault(x => x.User == chatSessionData.UserName)?.Text,
            Tokens = 0,
        });
    }

    public int GetTokenCount(string message)
    {
        return 0;
    }

    public void Dispose()
    {
    }
}
