using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;

namespace Voxta.Services.Mocks;

public class MockTextGenService : ITextGenService
{
    public string ServiceName => MockConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW };
    
    public Task<bool> TryInitializeAsync(Guid serviceId, string[] prerequisites, string culture, bool dry,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public (List<ChatMessageData> Messages, int Tokens)? GetMessagesToSummarize(IChatInferenceData chat)
    {
        return null;
    }

    public ValueTask<string> GenerateReplyAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        return GenerateAsync(chat.GetMessages().LastOrDefault(x => x.User == chat.User.Name.Value)?.Value ?? "", cancellationToken);
    }

    public ValueTask<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult("Echo: " + prompt);
    }

    public int GetTokenCount(string message)
    {
        return 0;
    }

    public void Dispose()
    {
    }
}
