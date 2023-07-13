using ChatMate.Abstractions.Diagnostics;
using ChatMate.Abstractions.Model;
using ChatMate.Abstractions.Repositories;
using ChatMate.Abstractions.Services;
using ChatMate.Common;
using Microsoft.DeepDev;

namespace ChatMate.Services.OpenAI;

public class OpenAITextGenClient : OpenAIClientBase, ITextGenService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ITokenizer _tokenizer;
    private readonly Sanitizer _sanitizer;
    private readonly IPerformanceMetrics _performanceMetrics;

    public OpenAITextGenClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, ITokenizer tokenizer, Sanitizer sanitizer, IPerformanceMetrics performanceMetrics)
        : base(httpClientFactory)
    {
        httpClientFactory.CreateClient($"{OpenAIConstants.ServiceName}.TextGen");
        _settingsRepository = settingsRepository;
        _tokenizer = tokenizer;
        _sanitizer = sanitizer;
        _performanceMetrics = performanceMetrics;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<OpenAISettings>(cancellationToken);
        InitializeClient(settings);
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
}