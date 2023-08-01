using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;

namespace Voxta.Services.Mocks;

public class MockActionInferenceService : IActionInferenceService
{
    public string ServiceName => MockConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW };
    
    public Task<bool> InitializeAsync(string[] prerequisites, string culture, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public ValueTask<string> SelectActionAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        if(chat.Actions is null || chat.Actions.Length == 0)
            return ValueTask.FromResult("idle");
        var action = chat.Actions[Random.Shared.Next(chat.Actions.Length)];
        return ValueTask.FromResult(action);
    }

    public void Dispose()
    {
    }
}