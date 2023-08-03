using Microsoft.DeepDev;
using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Abstractions.System;

namespace Voxta.Services.OpenAI;

public class OpenAIActionInferenceClient : OpenAIClientBase, IActionInferenceService
{
    public string ServiceName => OpenAIConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW, ServiceFeatures.GPT3 };
    
    private readonly ITokenizer _tokenizer;
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IServiceObserver _serviceObserver;

    public OpenAIActionInferenceClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, ITokenizer tokenizer, IPerformanceMetrics performanceMetrics, ILocalEncryptionProvider encryptionProvider, IServiceObserver serviceObserver)
        : base(httpClientFactory, settingsRepository, tokenizer, encryptionProvider)
    {
        httpClientFactory.CreateClient($"{OpenAIConstants.ServiceName}.ActionInference");
        _tokenizer = tokenizer;
        _performanceMetrics = performanceMetrics;
        _serviceObserver = serviceObserver;
    }

    public async ValueTask<string> SelectActionAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        if(chat.Actions is null || chat.Actions.Length == 0)
            return "idle";
        if (chat.Actions.Length == 1) return chat.Actions[0];

        var actionInferencePerf = _performanceMetrics.Start("OpenAI.ActionInference");
        var builder = new OpenAIPromptBuilder(_tokenizer);
        var messages = builder.BuildActionInferencePrompt(chat);
        foreach(var message in messages)
        {
            _serviceObserver.Record($"OpenAI.ActionInference.Message[{message.role}]", message.content);
        }

        var action = await SendChatRequestAsync(BuildRequestBody(messages), cancellationToken);
        actionInferencePerf.Done();
        var result = action.Trim('\'', '"', '.', '[', ']', ' ').ToLowerInvariant();
        _serviceObserver.Record("OpenAI.ActionInference.Value", result);
        return result;
    }
}
