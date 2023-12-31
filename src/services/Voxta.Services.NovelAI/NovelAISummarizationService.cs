﻿using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Abstractions.System;

namespace Voxta.Services.NovelAI;

public class NovelAISummarizationService : NovelAIClientBase, ISummarizationService
{
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IServiceObserver _serviceObserver;

    public NovelAISummarizationService(ISettingsRepository settingsRepository, IHttpClientFactory httpClientFactory, IPerformanceMetrics performanceMetrics, ILocalEncryptionProvider encryptionProvider, IServiceObserver serviceObserver)
        : base(settingsRepository, httpClientFactory, encryptionProvider)
    {
        _performanceMetrics = performanceMetrics;
        _serviceObserver = serviceObserver;
    }

    protected override bool ValidateSettings(NovelAISettings settings)
    {
        return settings.Model != NovelAISettings.ClioV1;
    }

    public async ValueTask<string> SummarizeAsync(IChatInferenceData chat, IReadOnlyList<ChatMessageData> messagesToSummarize, CancellationToken cancellationToken)
    {
        var builder = new NovelAIPromptBuilder(Tokenizer);
        var prompt = builder.BuildSummarizationPromptString(chat, messagesToSummarize);
        _serviceObserver.Record(ServiceObserverKeys.SummarizationService, NovelAIConstants.ServiceName);
        _serviceObserver.Record(ServiceObserverKeys.SummarizationPrompt, prompt);

        var actionInferencePerf = _performanceMetrics.Start($"{NovelAIConstants.ServiceName}.Summarization");
        var body = BuildRequestBody(prompt, "special_instruct");
        body.Parameters.Temperature = 0.1;
        body.Parameters.StopSequences = Array.Empty<int[]>();
        body.Parameters.MaxLength = Settings.SummaryMaxTokens;
        var action = await SendCompletionRequest(body, cancellationToken);
        actionInferencePerf.Done();

        var result = action.TrimContainerAndToLower();
        _serviceObserver.Record(ServiceObserverKeys.SummarizationResult, result);
        return result;
    }
}