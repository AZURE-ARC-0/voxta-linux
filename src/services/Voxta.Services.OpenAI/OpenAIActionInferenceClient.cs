using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Abstractions.System;

namespace Voxta.Services.OpenAI;

public class OpenAIActionInferenceClient : OpenAIClientBase, IActionInferenceService
{
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IServiceObserver _serviceObserver;

    public OpenAIActionInferenceClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, IPerformanceMetrics performanceMetrics, ILocalEncryptionProvider encryptionProvider, IServiceObserver serviceObserver)
        : base(httpClientFactory, settingsRepository, encryptionProvider)
    {
        httpClientFactory.CreateClient($"{OpenAIConstants.ServiceName}.ActionInference");
        _performanceMetrics = performanceMetrics;
        _serviceObserver = serviceObserver;
    }

    public async ValueTask<string> SelectActionAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        if(chat.Actions is null || chat.Actions.Length == 0)
            return "idle";
        if (chat.Actions.Length == 1) return chat.Actions[0];

        var actionInferencePerf = _performanceMetrics.Start("OpenAI.ActionInference");
        var builder = new OpenAIPromptBuilder(Tokenizer);
        var messages = builder.BuildActionInferencePrompt(chat);
        _serviceObserver.Record(ServiceObserverKeys.ActionInferenceService, OpenAIConstants.ServiceName);
        foreach(var message in messages)
        {
            _serviceObserver.Record($"{ServiceObserverKeys.ActionInferencePrompt}[{message.role}]", message.content);
        }

        var body = BuildRequestBody(messages);
        body.Stop = new[] { "]" };
        var action = await SendChatRequestAsync(body, cancellationToken);
        actionInferencePerf.Done();
        var result = action.Trim('\'', '"', '.', '[', ']', ' ').ToLowerInvariant();
        _serviceObserver.Record(ServiceObserverKeys.ActionInferenceResult, result);
        return result;
    }
}
