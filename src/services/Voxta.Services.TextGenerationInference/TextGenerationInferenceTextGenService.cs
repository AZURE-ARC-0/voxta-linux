﻿using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Shared.LLMUtils;

namespace Voxta.Services.TextGenerationInference;

public class TextGenerationInferenceTextGenService : TextGenerationInferenceClientBase, ITextGenService
{
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IServiceObserver _serviceObserver;

    public TextGenerationInferenceTextGenService(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, IPerformanceMetrics performanceMetrics, IServiceObserver serviceObserver)
        : base(httpClientFactory, settingsRepository)
    {
        _performanceMetrics = performanceMetrics;
        _serviceObserver = serviceObserver;
    }

    public async ValueTask<string> GenerateReplyAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        var builder = TextPromptBuilderFactory.Create(Settings.PromptFormat, Tokenizer);
        var prompt = builder.BuildReplyPromptString(chat, Settings.MaxMemoryTokens, Settings.MaxContextTokens);
        _serviceObserver.Record(ServiceObserverKeys.TextGenService, TextGenerationInferenceConstants.ServiceName);
        _serviceObserver.Record(ServiceObserverKeys.TextGenPrompt, prompt);
        
        var textGenPerf = _performanceMetrics.Start("TextGenerationInference.TextGen");
        var stoppingStrings = builder.GetReplyStoppingStrings(chat);;
        var text = await SendCompletionRequest(BuildRequestBody(prompt, stoppingStrings), cancellationToken);
        textGenPerf.Done();
        
        _serviceObserver.Record(ServiceObserverKeys.TextGenResult, text);
        return text;
    }

    public async ValueTask<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        var body = BuildRequestBody(prompt, new[] { "\n" });
        return await SendCompletionRequest(body, cancellationToken);
    }
}