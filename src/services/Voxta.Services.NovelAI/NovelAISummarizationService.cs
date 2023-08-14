using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Abstractions.System;
using Voxta.Services.OpenSourceLargeLanguageModels;

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

    public async ValueTask<string> SummarizeAsync(IChatInferenceData chat, List<ChatMessageData> messagesToSummarize, CancellationToken cancellationToken)
    {
        var builder = new NovelAIPromptBuilder(Tokenizer);
        var prompt = builder.BuildSummarizationPrompt(chat, messagesToSummarize);
        _serviceObserver.Record(ServiceObserverKeys.SummarizationService, NovelAIConstants.ServiceName);
        _serviceObserver.Record(ServiceObserverKeys.SummarizationPrompt, prompt);

        var actionInferencePerf = _performanceMetrics.Start($"{NovelAIConstants.ServiceName}.Summarization");
        var body = BuildRequestBody(prompt, "special_instruct");
        #warning Move this value elsewhere
        const int summarizeToMaxTokens = 60;
        body.Parameters.MaxLength = summarizeToMaxTokens;
        body.Parameters.Temperature = 0.1;
        var action = await SendCompletionRequest(body, cancellationToken);
        actionInferencePerf.Done();

        var result = action.TrimContainerAndToLower();
        _serviceObserver.Record(ServiceObserverKeys.SummarizationResult, result);
        return result;
    }
}