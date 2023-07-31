using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Shared.LargeLanguageModelsUtils;

namespace Voxta.Services.Oobabooga;

public class OobaboogaActionInferenceClient : OobaboogaClientBase, IActionInferenceService
{
    public string ServiceName => OobaboogaConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW };
    
    private readonly IPerformanceMetrics _performanceMetrics;

    public OobaboogaActionInferenceClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, IPerformanceMetrics performanceMetrics)
        :base(httpClientFactory, settingsRepository)
    {
        _performanceMetrics = performanceMetrics;
    }

    public async ValueTask<string> SelectActionAsync(ChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        var builder = new GenericPromptBuilder();
        var prompt = builder.BuildActionInferencePrompt(chatSessionData);
        
        var actionInferencePerf = _performanceMetrics.Start($"{OobaboogaConstants.ServiceName}.ActionInference");
        var animation = await SendCompletionRequest(prompt, new[] { "]" }, cancellationToken);
        actionInferencePerf.Done();
        return animation.Trim('\'', '"', '.', '[', ']', ' ').ToLowerInvariant();
    }
}