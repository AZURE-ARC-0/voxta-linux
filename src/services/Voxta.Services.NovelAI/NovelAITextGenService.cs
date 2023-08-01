using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Services.OpenSourceLargeLanguageModels;

namespace Voxta.Services.NovelAI;

public class NovelAITextGenService : NovelAIClientBase, ITextGenService
{
    private readonly IPerformanceMetrics _performanceMetrics;

    public NovelAITextGenService(ISettingsRepository settingsRepository, IHttpClientFactory httpClientFactory, IPerformanceMetrics performanceMetrics)
        : base(settingsRepository, httpClientFactory)
    {
        _performanceMetrics = performanceMetrics;
    }

    public async ValueTask<string> GenerateReplyAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        var builder = new NovelAIPromptBuilder();
        var textGenPerf = _performanceMetrics.Start("NovelAI.TextGen");
        var input = builder.BuildReplyPrompt(chat, includePostHistoryPrompt: false);
        var text = await SendCompletionRequest(input, "vanilla", cancellationToken);
        textGenPerf.Done();
        return text;
    }
}