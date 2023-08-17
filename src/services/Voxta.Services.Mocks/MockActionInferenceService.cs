using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;

namespace Voxta.Services.Mocks;

public class MockActionInferenceService : MockServiceBase, IActionInferenceService
{
    public MockActionInferenceService(ISettingsRepository settingsRepository) : base(settingsRepository)
    {
    }

    public ValueTask<string> SelectActionAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        if(chat.Actions is null || chat.Actions.Length == 0)
            return ValueTask.FromResult("idle");
        var action = chat.Actions[Random.Shared.Next(chat.Actions.Length)];
        return ValueTask.FromResult(action);
    }
}