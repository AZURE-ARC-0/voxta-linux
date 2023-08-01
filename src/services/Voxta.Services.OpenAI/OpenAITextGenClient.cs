using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Microsoft.DeepDev;

namespace Voxta.Services.OpenAI;

public class OpenAITextGenClient : OpenAIClientBase, ITextGenService
{
    public string ServiceName => OpenAIConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.GPT3 };
    
    private readonly ITokenizer _tokenizer;
    private readonly IPerformanceMetrics _performanceMetrics;

    public OpenAITextGenClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, ITokenizer tokenizer, IPerformanceMetrics performanceMetrics)
        : base(httpClientFactory, settingsRepository, tokenizer)
    {
        httpClientFactory.CreateClient($"{OpenAIConstants.ServiceName}.TextGen");
        _tokenizer = tokenizer;
        _performanceMetrics = performanceMetrics;
    }

    public override Task<bool> InitializeAsync(string[] prerequisites, string culture, CancellationToken cancellationToken)
    {
        if (prerequisites.Contains(ServiceFeatures.NSFW)) return Task.FromResult(false);
        return base.InitializeAsync(prerequisites, culture, cancellationToken);
    }

    public async ValueTask<string> GenerateReplyAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        var builder = new OpenAIPromptBuilder(_tokenizer);
        
        var tokenizePerf = _performanceMetrics.Start("OpenAI.PromptBuilder");

        var messages = builder.BuildReplyPrompt(chat, 4096);

        tokenizePerf.Pause();

        var textGenPerf = _performanceMetrics.Start("OpenAI.TextGen");
        var reply = await SendChatRequestAsync(messages, cancellationToken);
        textGenPerf.Done();

        return reply;
    }
}