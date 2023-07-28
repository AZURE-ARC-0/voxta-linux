using Microsoft.DeepDev;
using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;

namespace Voxta.Services.OpenAI;

public class OpenAIActionInferenceClient : OpenAIClientBase, IActionInferenceService
{
    public string ServiceName => OpenAIConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW, ServiceFeatures.GPT3 };
    
    private readonly ITokenizer _tokenizer;
    private readonly IPerformanceMetrics _performanceMetrics;

    public OpenAIActionInferenceClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, ITokenizer tokenizer, IPerformanceMetrics performanceMetrics)
        : base(httpClientFactory, settingsRepository)
    {
        httpClientFactory.CreateClient($"{OpenAIConstants.ServiceName}.ActionInference");
        _tokenizer = tokenizer;
        _performanceMetrics = performanceMetrics;
    }

    public int GetTokenCount(string message)
    {
        if (string.IsNullOrEmpty(message)) return 0;
        return _tokenizer.Encode(message, OpenAISpecialTokens.Keys).Count;
    }

    public async ValueTask<string> SelectActionAsync(ChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        if(chatSessionData.Actions is null || chatSessionData.Actions.Length == 0)
            return "idle";
        if (chatSessionData.Actions.Length == 1) return chatSessionData.Actions[0];

        var actionInferencePerf = _performanceMetrics.Start("OpenAI.ActionInference");
        var builder = new OpenAIPromptBuilder(null);
        var messages = builder.BuildActionInferencePrompt(chatSessionData);
        
        var animation = await SendChatRequestAsync(messages, cancellationToken);
        actionInferencePerf.Done();
        return animation.Trim('\'', '"', '.', '[', ']', ' ').ToLowerInvariant();
    }

    public void Dispose()
    {
    }
}