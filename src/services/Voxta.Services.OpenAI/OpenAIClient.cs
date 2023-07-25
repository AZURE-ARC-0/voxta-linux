using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Common;
using Microsoft.DeepDev;

namespace Voxta.Services.OpenAI;

public class OpenAIClient : OpenAIClientBase, ITextGenService, IActionInferenceService
{
    private readonly ITokenizer _tokenizer;
    private readonly Sanitizer _sanitizer;
    private readonly IPerformanceMetrics _performanceMetrics;

    public OpenAIClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, ITokenizer tokenizer, Sanitizer sanitizer, IPerformanceMetrics performanceMetrics)
        : base(httpClientFactory, settingsRepository)
    {
        httpClientFactory.CreateClient($"{OpenAIConstants.ServiceName}.TextGen");
        _tokenizer = tokenizer;
        _sanitizer = sanitizer;
        _performanceMetrics = performanceMetrics;
    }

    public int GetTokenCount(string message)
    {
        if (string.IsNullOrEmpty(message)) return 0;
        return _tokenizer.Encode(message, OpenAISpecialTokens.Keys).Count;
    }

    public async ValueTask<TextData> GenerateReplyAsync(IReadOnlyChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        var builder = new OpenAIPromptBuilder(_tokenizer);
        
        var tokenizePerf = _performanceMetrics.Start("OpenAI.PromptBuilder");

        var messages = builder.BuildReplyPrompt(chatSessionData, 4096);

        tokenizePerf.Pause();

        var textGenPerf = _performanceMetrics.Start("OpenAI.TextGen");
        var reply = await SendChatRequestAsync(messages, cancellationToken);
        textGenPerf.Done();
        
        var sanitized = _sanitizer.Sanitize(reply);
        
        tokenizePerf.Resume();
        var tokens = _tokenizer.Encode(sanitized, OpenAISpecialTokens.Keys);
        tokenizePerf.Done();
        
        return new TextData
        {
            Text = sanitized,
            Tokens = tokens.Count
        };
    }

    public async ValueTask<string> SelectActionAsync(ChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        if(chatSessionData.Actions is null || chatSessionData.Actions.Length == 0)
            return "idle";
        if (chatSessionData.Actions.Length == 1) return chatSessionData.Actions[0];

        var builder = new OpenAIPromptBuilder(null);
        var messages = builder.BuildActionInferencePrompt(chatSessionData);
        
        var animation = await SendChatRequestAsync(messages, cancellationToken);
        return animation.Trim('\'', '"', '.', '[', ']', ' ').ToLowerInvariant();
    }

    public void Dispose()
    {
    }
}