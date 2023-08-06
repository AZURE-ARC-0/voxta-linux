using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Abstractions.System;

namespace Voxta.Services.OpenAI;

public class OpenAITextGenClient : OpenAIClientBase, ITextGenService
{
    public string ServiceName => OpenAIConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.GPT3 };
    
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IServiceObserver _serviceObserver;

    public OpenAITextGenClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, IPerformanceMetrics performanceMetrics, ILocalEncryptionProvider encryptionProvider, IServiceObserver serviceObserver)
        : base(httpClientFactory, settingsRepository, encryptionProvider)
    {
        httpClientFactory.CreateClient($"{OpenAIConstants.ServiceName}.TextGen");
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
        var builder = new OpenAIPromptBuilder(Tokenizer);
        var tokenizePerf = _performanceMetrics.Start("OpenAI.PromptBuilder");
        var messages = builder.BuildReplyPrompt(chat, MaxMemoryTokens, MaxContextTokens);
        _serviceObserver.Record(ServiceObserverKeys.TextGenService, OpenAIConstants.ServiceName);
        _serviceObserver.Record(ServiceObserverKeys.TextGenPrompt, "");
        tokenizePerf.Pause();

        var textGenPerf = _performanceMetrics.Start("OpenAI.TextGen");
        var text = await SendChatRequestAsync(BuildRequestBody(messages), cancellationToken);
        textGenPerf.Done();

        _serviceObserver.Record(ServiceObserverKeys.TextGenResult, text);
        return text;
    }

    public ValueTask<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        _serviceObserver.Record(ServiceObserverKeys.TextGenService, OpenAIConstants.ServiceName);
        _serviceObserver.Record(ServiceObserverKeys.TextGenPrompt, prompt);
        _serviceObserver.Record(ServiceObserverKeys.TextGenResult, "");
        throw new NotSupportedException("Raw prompt generation is not supported by OpenAI TextGen");
    }
}