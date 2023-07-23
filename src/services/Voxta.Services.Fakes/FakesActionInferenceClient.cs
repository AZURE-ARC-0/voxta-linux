using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;

namespace Voxta.Services.Fakes;

public class FakesActionInferenceClient : IActionInferenceService
{
    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public ValueTask<string> SelectActionAsync(ChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        if(chatSessionData.Actions is null || chatSessionData.Actions.Length == 0)
            return ValueTask.FromResult("idle");
        var action = chatSessionData.Actions[Random.Shared.Next(chatSessionData.Actions.Length)];
        return ValueTask.FromResult(action);
    }

    public void Dispose()
    {
    }
}