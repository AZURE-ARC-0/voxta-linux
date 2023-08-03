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
        var prompt = builder.BuildReplyPrompt(chat, MaxContextTokens);
        _serviceObserver.Record("KoboldAI.TextGen.Prompt", prompt);
        
        var textGenPerf = _performanceMetrics.Start("KoboldAI.TextGen");
        var stoppingStrings = new[] { "END_OF_DIALOG", $"{chat.UserName}:", $"{chat.Character.Name}:", "\n" };
        var text = await SendCompletionRequest(BuildRequestBody(prompt, stoppingStrings), cancellationToken);
        textGenPerf.Done();
        
        _serviceObserver.Record("KoboldAI.TextGen.Reply", text);
        return text;
    }
}