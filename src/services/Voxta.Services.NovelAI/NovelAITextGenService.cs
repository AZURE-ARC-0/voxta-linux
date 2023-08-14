using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Abstractions.System;

namespace Voxta.Services.NovelAI;

public class NovelAITextGenService : NovelAIClientBase, ITextGenService
{
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IServiceObserver _serviceObserver;

    public NovelAITextGenService(ISettingsRepository settingsRepository, IHttpClientFactory httpClientFactory, IPerformanceMetrics performanceMetrics, ILocalEncryptionProvider encryptionProvider, IServiceObserver serviceObserver)
        : base(settingsRepository, httpClientFactory, encryptionProvider)
    {
        _performanceMetrics = performanceMetrics;
        _serviceObserver = serviceObserver;
    }

    public async ValueTask<string> GenerateReplyAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        var builder = new NovelAIPromptBuilder(Tokenizer);
        var prompt = builder.BuildReplyPrompt(chat, Settings.MaxMemoryTokens, Settings.MaxContextTokens, includePostHistoryPrompt: false);
        _serviceObserver.Record(ServiceObserverKeys.TextGenService, NovelAIConstants.ServiceName);
        _serviceObserver.Record(ServiceObserverKeys.TextGenPrompt, prompt);
        var textGenPerf = _performanceMetrics.Start("NovelAI.TextGen");
        var text = await SendCompletionRequest(BuildRequestBody(prompt, "special_instruct"), cancellationToken);
        textGenPerf.Done();
        
        _serviceObserver.Record(ServiceObserverKeys.TextGenResult, text);
        return text;
    }

    public async ValueTask<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        _serviceObserver.Record(ServiceObserverKeys.TextGenService, NovelAIConstants.ServiceName);
        _serviceObserver.Record(ServiceObserverKeys.TextGenPrompt, prompt);
        var text = await SendCompletionRequest(BuildRequestBody(prompt, "special_instruct"), cancellationToken);
        _serviceObserver.Record(ServiceObserverKeys.TextGenResult, text);
        return text;
    }
}