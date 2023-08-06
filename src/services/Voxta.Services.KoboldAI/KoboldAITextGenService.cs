using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Shared.LargeLanguageModelsUtils;

namespace Voxta.Services.KoboldAI;

public class KoboldAITextGenService : KoboldAIClientBase, ITextGenService
{
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IServiceObserver _serviceObserver;

    public KoboldAITextGenService(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, IPerformanceMetrics performanceMetrics, IServiceObserver serviceObserver)
        : base(httpClientFactory, settingsRepository)
    {
        _performanceMetrics = performanceMetrics;
        _serviceObserver = serviceObserver;
    }

    public async ValueTask<string> GenerateReplyAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        var builder = new GenericPromptBuilder(Tokenizer);
        var prompt = builder.BuildReplyPrompt(chat, MaxMemoryTokens, MaxContextTokens);
        _serviceObserver.Record(ServiceObserverKeys.TextGenService, KoboldAIConstants.ServiceName);
        _serviceObserver.Record(ServiceObserverKeys.TextGenPrompt, prompt);
        
        var textGenPerf = _performanceMetrics.Start("KoboldAI.TextGen");
        var stoppingStrings = new[] { "END_OF_DIALOG", $"{chat.UserName}:", $"{chat.Character.Name}:", "\n" };
        var text = await SendCompletionRequest(BuildRequestBody(prompt, stoppingStrings), cancellationToken);
        textGenPerf.Done();
        
        _serviceObserver.Record(ServiceObserverKeys.TextGenResult, text);
        return text;
    }

    public async ValueTask<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        _serviceObserver.Record(ServiceObserverKeys.TextGenService, KoboldAIConstants.ServiceName);
        _serviceObserver.Record(ServiceObserverKeys.TextGenPrompt, prompt);
        var text = await SendCompletionRequest(BuildRequestBody(prompt, new[] { "\n" }), cancellationToken);
        _serviceObserver.Record(ServiceObserverKeys.TextGenResult, text);
        return text;
    }
}