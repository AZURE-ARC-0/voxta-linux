using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Microsoft.DeepDev;
using Voxta.Abstractions.System;

namespace Voxta.Services.OpenAI;

public class OpenAITextGenClient : OpenAIClientBase, ITextGenService
{
    public string ServiceName => OpenAIConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.GPT3 };
    
    private readonly ITokenizer _tokenizer;
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IServiceObserver _serviceObserver;

    public OpenAITextGenClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, ITokenizer tokenizer, IPerformanceMetrics performanceMetrics, ILocalEncryptionProvider encryptionProvider, IServiceObserver serviceObserver)
        : base(httpClientFactory, settingsRepository, tokenizer, encryptionProvider)
    {
        httpClientFactory.CreateClient($"{OpenAIConstants.ServiceName}.TextGen");
        _tokenizer = tokenizer;
        _performanceMetrics = performanceMetrics;
        _serviceObserver = serviceObserver;
    }

    public override Task<bool> TryInitializeAsync(string[] prerequisites, string culture, bool dry, CancellationToken cancellationToken)
    {
        if (prerequisites.Contains(ServiceFeatures.NSFW)) return Task.FromResult(false);
        return base.TryInitializeAsync(prerequisites, culture, dry, cancellationToken);
    }

    public async ValueTask<string> GenerateReplyAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        var builder = new OpenAIPromptBuilder(_tokenizer);
        var tokenizePerf = _performanceMetrics.Start("OpenAI.PromptBuilder");
        var messages = builder.BuildReplyPrompt(chat, MaxContextTokens);
        tokenizePerf.Pause();

        var textGenPerf = _performanceMetrics.Start("OpenAI.TextGen");
        var text = await SendChatRequestAsync(BuildRequestBody(messages), cancellationToken);
        textGenPerf.Done();

        _serviceObserver.Record("OpenAI.TextGen.Reply", text);
        return text;
    }
}