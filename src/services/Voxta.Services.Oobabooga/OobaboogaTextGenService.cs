using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Shared.LargeLanguageModelsUtils;

namespace Voxta.Services.Oobabooga;

public class OobaboogaTextGenService : OobaboogaClientBase, ITextGenService
{
    public string ServiceName => OobaboogaConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW };
    
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
        var builder = new GenericPromptBuilder(Tokenizer);
        var prompt = builder.BuildReplyPrompt(chat, MaxMemoryTokens, MaxContextTokens);
        _serviceObserver.Record(ServiceObserverKeys.TextGenService, OobaboogaConstants.ServiceName);
        _serviceObserver.Record(ServiceObserverKeys.TextGenPrompt, prompt);
        
        var textGenPerf = _performanceMetrics.Start("KoboldAI.TextGen");
        var stoppingStrings = new[] { "END_OF_DIALOG", "You:", $"{chat.UserName}:", $"{chat.Character.Name}:", "\n" };
        var text = await SendCompletionRequest(BuildRequestBody(prompt, stoppingStrings), cancellationToken);
        textGenPerf.Done();
        
        _serviceObserver.Record(ServiceObserverKeys.TextGenResult, text);
        return text;
    }

    public async ValueTask<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        _serviceObserver.Record(ServiceObserverKeys.TextGenService, OobaboogaConstants.ServiceName);
        _serviceObserver.Record(ServiceObserverKeys.TextGenPrompt, prompt);
        var text = await SendCompletionRequest(BuildRequestBody(prompt, new[] { "\n" }), cancellationToken);
        _serviceObserver.Record(ServiceObserverKeys.TextGenResult, text);
        return text;
    }
}