using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Services;

namespace ChatMate.Services.Fakes;

public class FakesAnimationSelectionClient : IAnimationSelectionService
{
    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public ValueTask<string> SelectAnimationAsync(ChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult("idle");
    }
}