using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Shared.LLMUtils;

namespace Voxta.Services.KoboldAI;

public class KoboldAISummarizationService : KoboldAIClientBase, ISummarizationService
{
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IServiceObserver _serviceObserver;

    public KoboldAISummarizationService(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, IPerformanceMetrics performanceMetrics, IServiceObserver serviceObserver)
        :base(httpClientFactory, settingsRepository)
    {
        _performanceMetrics = performanceMetrics;
        _serviceObserver = serviceObserver;
    }

    public async ValueTask<string> SummarizeAsync(IChatInferenceData chat, List<ChatMessageData> messagesToSummarize, CancellationToken cancellationToken)
    {
        var builder = new GenericPromptBuilder(Tokenizer);
        var prompt = builder.BuildSummarizationPrompt(chat, messagesToSummarize);
        _serviceObserver.Record(ServiceObserverKeys.SummarizationService, KoboldAIConstants.ServiceName);
        _serviceObserver.Record(ServiceObserverKeys.SummarizationPrompt, prompt);
        
        var actionInferencePerf = _performanceMetrics.Start($"{KoboldAIConstants.ServiceName}.Summarization");
        var body = BuildRequestBody(prompt, builder.SummarizationStopTokens);
        body.Temperature = 0.1;
        body.StopSequence = Array.Empty<string>();
        var action = await SendCompletionRequest(body, cancellationToken);
        actionInferencePerf.Done();

        var result = action.TrimExcess();
        _serviceObserver.Record(ServiceObserverKeys.SummarizationResult, result);
        return result;
    }
}