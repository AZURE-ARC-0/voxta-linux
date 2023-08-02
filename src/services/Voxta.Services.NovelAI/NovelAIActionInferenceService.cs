using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Abstractions.System;
using Voxta.Services.OpenSourceLargeLanguageModels;

namespace Voxta.Services.NovelAI;

public class NovelAIActionInferenceService : NovelAIClientBase, IActionInferenceService
{
    private readonly IPerformanceMetrics _performanceMetrics;

    public NovelAIActionInferenceService(ISettingsRepository settingsRepository, IHttpClientFactory httpClientFactory, IPerformanceMetrics performanceMetrics, ILocalEncryptionProvider encryptionProvider)
        : base(settingsRepository, httpClientFactory, encryptionProvider)
    {
        _performanceMetrics = performanceMetrics;
    }

    protected override bool ValidateSettings(NovelAISettings settings)
    {
        return settings.Model != "clio-v1";
    }

    public async ValueTask<string> SelectActionAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        var builder = new NovelAIPromptBuilder(Tokenizer);
        var actionInferencePerf = _performanceMetrics.Start($"{NovelAIConstants.ServiceName}.ActionInference");
        var input = builder.BuildActionInferencePrompt(chat);
        var action = await SendCompletionRequest(input, "special_instruct", cancellationToken);
        actionInferencePerf.Done();
        return action.TrimContainedToLower();
    }
}