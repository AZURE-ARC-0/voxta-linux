using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Common;
using Microsoft.DeepDev;

namespace Voxta.Services.OpenAI;

public class OpenAITextGenClient : OpenAIClientBase, ITextGenService
{
    public string ServiceName => OpenAIConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.GPT3 };
    
    private readonly ITokenizer _tokenizer;
    private readonly Sanitizer _sanitizer;
    private readonly IPerformanceMetrics _performanceMetrics;

    public OpenAITextGenClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, ITokenizer tokenizer, Sanitizer sanitizer, IPerformanceMetrics performanceMetrics)
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

    public override Task<bool> InitializeAsync(string[] prerequisites, string culture, CancellationToken cancellationToken)
    {
        if (prerequisites.Contains(ServiceFeatures.NSFW)) return Task.FromResult(false);
        return base.InitializeAsync(prerequisites, culture, cancellationToken);
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
        
        #warning Move out, since we run this on every request anyway; then encode outside of this.
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
}