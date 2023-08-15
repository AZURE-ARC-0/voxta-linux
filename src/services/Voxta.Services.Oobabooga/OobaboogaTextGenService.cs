using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Shared.LLMUtils;

namespace Voxta.Services.Oobabooga;

public class OobaboogaTextGenService : OobaboogaClientBase, ITextGenService
{
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IServiceObserver _serviceObserver;

    public OobaboogaTextGenService(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, IPerformanceMetrics performanceMetrics, IServiceObserver serviceObserver)
    :base(httpClientFactory, settingsRepository)
    {
        _performanceMetrics = performanceMetrics;
        _serviceObserver = serviceObserver;
    }

    public async ValueTask<string> GenerateReplyAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        var builder = TextPromptBuilderFactory.Create(Settings.PromptFormat, Tokenizer);
        var prompt = builder.BuildReplyPromptString(chat, Settings.MaxMemoryTokens, Settings.MaxContextTokens);
        _serviceObserver.Record(ServiceObserverKeys.TextGenService, OobaboogaConstants.ServiceName);
        _serviceObserver.Record(ServiceObserverKeys.TextGenPrompt, prompt);
        
        var textGenPerf = _performanceMetrics.Start("KoboldAI.TextGen");
        var stoppingStrings = builder.GetReplyStoppingStrings(chat);
        var body = BuildRequestBody(prompt, stoppingStrings);
        var text = await SendCompletionRequest(body, cancellationToken);
        textGenPerf.Done();
        
        _serviceObserver.Record(ServiceObserverKeys.TextGenResult, text);
        return text;
    }

    public async ValueTask<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        _serviceObserver.Record(ServiceObserverKeys.TextGenService, OobaboogaConstants.ServiceName);
        _serviceObserver.Record(ServiceObserverKeys.TextGenPrompt, prompt);
        var body = BuildRequestBody(prompt, Array.Empty<string>());
        var text = await SendCompletionRequest(body, cancellationToken);
        _serviceObserver.Record(ServiceObserverKeys.TextGenResult, text);
        return text;
    }
}