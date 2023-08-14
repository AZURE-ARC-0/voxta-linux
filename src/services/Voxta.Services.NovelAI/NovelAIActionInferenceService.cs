using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Abstractions.System;

namespace Voxta.Services.NovelAI;

public class NovelAIActionInferenceService : NovelAIClientBase, IActionInferenceService
{
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IServiceObserver _serviceObserver;

    public NovelAIActionInferenceService(ISettingsRepository settingsRepository, IHttpClientFactory httpClientFactory, IPerformanceMetrics performanceMetrics, ILocalEncryptionProvider encryptionProvider, IServiceObserver serviceObserver)
        : base(settingsRepository, httpClientFactory, encryptionProvider)
    {
        _performanceMetrics = performanceMetrics;
        _serviceObserver = serviceObserver;
    }

    protected override bool ValidateSettings(NovelAISettings settings)
    {
        return settings.Model != NovelAISettings.ClioV1;
    }

    public async ValueTask<string> SelectActionAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        var builder = new NovelAIPromptBuilder(Tokenizer);
        var prompt = builder.BuildActionInferencePrompt(chat);
        _serviceObserver.Record(ServiceObserverKeys.ActionInferenceService, NovelAIConstants.ServiceName);
        _serviceObserver.Record(ServiceObserverKeys.ActionInferencePrompt, prompt);

        var actionInferencePerf = _performanceMetrics.Start($"{NovelAIConstants.ServiceName}.ActionInference");
        var action = await SendCompletionRequest(BuildRequestBody(prompt, "special_instruct"), cancellationToken);
        actionInferencePerf.Done();

        var result = action.TrimContainerAndToLower();
        _serviceObserver.Record(ServiceObserverKeys.ActionInferenceResult, result);
        return result;
    }
}