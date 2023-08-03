using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Shared.LargeLanguageModelsUtils;

namespace Voxta.Services.Oobabooga;

public class OobaboogaActionInferenceService : OobaboogaClientBase, IActionInferenceService
{
    public string ServiceName => OobaboogaConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW };
    
    private readonly IPerformanceMetrics _performanceMetrics;

    public OobaboogaActionInferenceService(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, IPerformanceMetrics performanceMetrics)
        :base(httpClientFactory, settingsRepository)
    {
        _performanceMetrics = performanceMetrics;
    }

    public async ValueTask<string> SelectActionAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        var builder = new GenericPromptBuilder(Tokenizer);
        var prompt = builder.BuildActionInferencePrompt(chat);
        
        var actionInferencePerf = _performanceMetrics.Start($"{OobaboogaConstants.ServiceName}.ActionInference");
        var action = await SendCompletionRequest(prompt, new[] { "]" }, cancellationToken);
        actionInferencePerf.Done();
        
        return action.TrimContainerAndToLower();
    }
}