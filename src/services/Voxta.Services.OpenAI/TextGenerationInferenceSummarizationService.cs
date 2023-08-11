using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Abstractions.System;

namespace Voxta.Services.OpenAI;

public class OpenAISummarizationService : OpenAIClientBase, ISummarizationService
{
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IServiceObserver _serviceObserver;

    public OpenAISummarizationService(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, IPerformanceMetrics performanceMetrics, ILocalEncryptionProvider encryptionProvider, IServiceObserver serviceObserver)
        : base(httpClientFactory, settingsRepository, encryptionProvider)
    {
        httpClientFactory.CreateClient($"{OpenAIConstants.ServiceName}.ActionInference");
        _performanceMetrics = performanceMetrics;
        _serviceObserver = serviceObserver;
    }

    public async ValueTask<string> SummarizeAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        var perf = _performanceMetrics.Start($"{OpenAIConstants.ServiceName}.Summarization");
        var builder = new OpenAIPromptBuilder(Tokenizer);
        var messages = builder.BuildSummarizationPrompt(chat);
        _serviceObserver.Record(ServiceObserverKeys.SummarizationService, OpenAIConstants.ServiceName);
        foreach(var message in messages)
        {
            _serviceObserver.Record($"{ServiceObserverKeys.SummarizationPrompt}[{message.role}]", message.content);
        }
        
        var result = await SendChatRequestAsync(BuildRequestBody(messages), cancellationToken);
        perf.Done();
        _serviceObserver.Record(ServiceObserverKeys.SummarizationResult, result);
        return result;
    }
}