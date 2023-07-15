using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Services;

namespace ChatMate.Services.Fakes;

public class FakesTextGenClient : ITextGenService
{
    public Task InitializeAsync(CancellationToken cancellationToken)
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
}
