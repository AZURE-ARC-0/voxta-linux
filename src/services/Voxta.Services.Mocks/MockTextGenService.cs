using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;

namespace Voxta.Services.Mocks;

public class MockTextGenService : MockServiceBase, ITextGenService
{
    public MockTextGenService(ISettingsRepository settingsRepository) : base(settingsRepository)
    {
    }

    public int SummarizationTriggerTokens => int.MaxValue;

    public int GetTokenCount(string message)
    {
        return 0;
    }

    public ValueTask<string> GenerateReplyAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        using var token = chat.GetReadToken();
        return GenerateAsync(token.Messages.LastOrDefault(x => x.User == chat.User.Name.Value)?.Value ?? "", cancellationToken);
    }

    public ValueTask<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult("Echo: " + prompt);
    }
}
