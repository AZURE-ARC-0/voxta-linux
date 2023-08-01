using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Shared.LargeLanguageModelsUtils;

namespace Voxta.Services.KoboldAI;

public class KoboldAIActionInferenceService : KoboldAIClientBase, IActionInferenceService
{
    private readonly IPerformanceMetrics _performanceMetrics;

    public KoboldAIActionInferenceService(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, IPerformanceMetrics performanceMetrics)
        :base(httpClientFactory, settingsRepository)
    {
        _performanceMetrics = performanceMetrics;
    }

    public async ValueTask<string> SelectActionAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        var builder = new GenericPromptBuilder();
        var prompt = builder.BuildActionInferencePrompt(chat);
        
        var actionInferencePerf = _performanceMetrics.Start($"{KoboldAIConstants.ServiceName}.ActionInference");
        var action = await SendCompletionRequest(prompt, new[] { "]" }, cancellationToken);
        actionInferencePerf.Done();
        
        return action.TrimContainedToLower();
    }
}