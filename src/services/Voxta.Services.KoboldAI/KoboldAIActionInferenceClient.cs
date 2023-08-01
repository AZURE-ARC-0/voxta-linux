using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Shared.LargeLanguageModelsUtils;

namespace Voxta.Services.KoboldAI;

public class KoboldAIActionInferenceClient : KoboldAIClientBase, IActionInferenceService
{
    private readonly IPerformanceMetrics _performanceMetrics;

    public KoboldAIActionInferenceClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, IPerformanceMetrics performanceMetrics)
        :base(httpClientFactory, settingsRepository)
    {
        _performanceMetrics = performanceMetrics;
    }

    public async ValueTask<string> SelectActionAsync(ChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        var builder = new GenericPromptBuilder();
        var prompt = builder.BuildActionInferencePrompt(chatSessionData);
        
        var actionInferencePerf = _performanceMetrics.Start($"{KoboldAIConstants.ServiceName}.ActionInference");
        var action = await SendCompletionRequest(prompt, new[] { "]" }, cancellationToken);
        actionInferencePerf.Done();
        
        return action.TrimContainedToLower();
    }
}