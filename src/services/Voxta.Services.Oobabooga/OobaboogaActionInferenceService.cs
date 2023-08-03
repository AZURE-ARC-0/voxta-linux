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
    private readonly IServiceObserver _serviceObserver;

    public OobaboogaActionInferenceService(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, IPerformanceMetrics performanceMetrics, IServiceObserver serviceObserver)
        :base(httpClientFactory, settingsRepository)
    {
        _performanceMetrics = performanceMetrics;
        _serviceObserver = serviceObserver;
    }

    public async ValueTask<string> SelectActionAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        var builder = new GenericPromptBuilder(Tokenizer);
        var prompt = builder.BuildActionInferencePrompt(chat);
        _serviceObserver.Record(ServiceObserverKeys.ActionInferenceService, OobaboogaConstants.ServiceName);
        _serviceObserver.Record(ServiceObserverKeys.ActionInferencePrompt, prompt);
        
        var actionInferencePerf = _performanceMetrics.Start($"{OobaboogaConstants.ServiceName}.ActionInference");
        var stoppingStrings = new[] { "]" };
        var action = await SendCompletionRequest(BuildRequestBody(prompt, stoppingStrings), cancellationToken);
        actionInferencePerf.Done();
        
        var result = action.TrimContainerAndToLower();
        _serviceObserver.Record(ServiceObserverKeys.ActionInferenceResult, result);
        return result;
    }
}