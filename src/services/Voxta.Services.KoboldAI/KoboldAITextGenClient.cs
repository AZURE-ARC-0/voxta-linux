using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Shared.LargeLanguageModelsUtils;

namespace Voxta.Services.KoboldAI;

public class KoboldAITextGenClient : KoboldAIClientBase, ITextGenService
{
    private readonly IPerformanceMetrics _performanceMetrics;

    public KoboldAITextGenClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, IPerformanceMetrics performanceMetrics)
        : base(httpClientFactory, settingsRepository)
    {
        _performanceMetrics = performanceMetrics;
    }

    public async ValueTask<string> GenerateReplyAsync(IReadOnlyChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        var builder = new GenericPromptBuilder();
        var prompt = builder.BuildReplyPrompt(chatSessionData, -1);
        var textGenPerf = _performanceMetrics.Start("KoboldAI.TextGen");
        var text = await SendCompletionRequest(
            prompt,
            new[] { "END_OF_DIALOG", "You:", $"{chatSessionData.UserName}:", $"{chatSessionData.Character.Name}:", "\n", "\"" },
            cancellationToken
        );
        textGenPerf.Done();
        return text;
    }
}